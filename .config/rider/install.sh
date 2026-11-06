#!/usr/bin/env bash
#
# Copies the Rider settings of the repository into this PC's Rider configuration (Git Bash on Windows).
# Building the solution runs it with --if-changed (Directory.Build.props), which installs only settings this configuration
# has not received yet, so a setting changed in Rider afterwards is not reverted by the next build. Without the option it
# always installs.
#
# Rider writes its settings back when it exits, which would undo a copy made while it runs. So while it runs, the script
# leaves a background copy of itself (--wait-for-exit) that installs the moment Rider exits, and asks for a restart.
#
# The solution itself carries what Rider reads from a solution: run configurations (.run), required plugins, inspection
# profile and dictionary (.idea), code style and severities (.editorconfig, Maroik.slnx.DotSettings). The keymap, color
# scheme, theme and editor options are IDE-level: Rider reads them only from its configuration folder, whatever solution
# is open, so they need this copy. settings/ mirrors that folder and holds no license, account or path of a PC.

set -euo pipefail

repository_settings="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exported_version="Rider2026.2"
is_windows=false

mode="${1:-}"
platform="$(uname -s)"

case "$platform" in
    Darwin) jetbrains="$HOME/Library/Application Support/JetBrains" ;;
    MINGW* | MSYS* | CYGWIN*)
        jetbrains="$APPDATA/JetBrains"
        is_windows=true
        ;;
    *) jetbrains="${XDG_CONFIG_HOME:-$HOME/.config}/JetBrains" ;;
esac

# The newest configuration of this PC. A PC whose Rider never ran has none: the exported version's folder is what a newer
# Rider offers to import on its first start.
newest="$(find "$jetbrains" -mindepth 1 -maxdepth 1 -type d -name 'Rider[0-9]*' 2>/dev/null | sort -V | tail -n 1 || true)"
config="${newest:-$jetbrains/$exported_version}"

# What the repository exports now, recorded in the configuration after an install.
settings_version() {
    find "$repository_settings/settings" "$repository_settings/ui-font-component.xml" -type f | LC_ALL=C sort |
        while IFS= read -r file; do cat "$file"; done | cksum
}

installed_version="$config/.maroik-rider-settings"
pending="$config/.maroik-rider-settings-pending"

if [[ "$mode" == "--if-changed" && -f "$installed_version" && "$(cat "$installed_version")" == "$(settings_version)" ]]; then
    exit 0
fi

# The folder's .lock holds its Rider's process id, and is left behind after the exit, so what counts is whether that process
# still runs. It is read once: the restarted Rider writes its own id there, and the install must not wait for that one.
rider="$(cat "$config/.lock" 2>/dev/null || true)"

rider_runs() {
    [[ -n "$rider" ]] || return 1
    if [[ "$is_windows" == true ]]; then
        # Git Bash's kill knows only its own processes.
        tasklist.exe //FI "PID eq $rider" | grep -qw "$rider"
    else
        kill -0 "$rider" 2>/dev/null
    fi
}

# A popup where the desktop offers one; the build output says the same either way.
show_popup() {
    local message="$1"
    case "$platform" in
        Darwin) osascript -e "display notification \"$message\" with title \"Rider\"" >/dev/null 2>&1 || true ;;
        MINGW* | MSYS* | CYGWIN*)
            powershell.exe -NoProfile -Command "Add-Type -AssemblyName PresentationFramework; [System.Windows.MessageBox]::Show('$message', 'Rider')" </dev/null >/dev/null 2>&1 &
            ;;
        *) notify-send "Rider" "$message" >/dev/null 2>&1 || true ;;
    esac
}

if rider_runs; then
    if [[ "$mode" == "--wait-for-exit" ]]; then
        while rider_runs; do sleep 0.2; done
    else
        restart_message="Rider settings are ready. Restart Rider to apply them."
        echo "$restart_message"
        # Every build reaches this line until the restart; one waiting install and one popup are enough.
        if [[ -f "$pending" ]] && kill -0 "$(cat "$pending")" 2>/dev/null; then
            exit 0
        fi
        mkdir -p "$config"
        nohup bash "${BASH_SOURCE[0]}" --wait-for-exit </dev/null >/dev/null 2>&1 &
        echo "$!" > "$pending"
        show_popup "$restart_message"
        exit 0
    fi
fi

backup="$config/settings-backup-$(date +%Y%m%d%H%M%S)"

back_up() {
    local relative="$1"
    mkdir -p "$(dirname "$backup/$relative")"
    cp "$config/$relative" "$backup/$relative"
}

while IFS= read -r source; do
    relative="${source#"$repository_settings/settings/"}"
    # The exported Docker connection names a Unix socket; on Windows Rider detects its own.
    if [[ "$is_windows" == true && "$relative" == "options/remote-servers.xml" ]]; then
        continue
    fi
    if [[ -e "$config/$relative" ]]; then
        cmp -s "$source" "$config/$relative" && continue
        back_up "$relative"
    fi
    mkdir -p "$(dirname "$config/$relative")"
    cp "$source" "$config/$relative"
done < <(find "$repository_settings/settings" -type f)

# other.xml mixes the UI font size with this PC's own state, so only the font component is replaced.
font="$repository_settings/ui-font-component.xml"
other="$config/options/other.xml"
mkdir -p "$(dirname "$other")"
if [[ -e "$other" ]]; then
    merged="$(awk -v font="$font" '
        /<component name="NotRoamableUiSettings"/ {
            if ($0 !~ /\/>[[:space:]]*$/ && $0 !~ /<\/component>/) skipping = 1
            next
        }
        skipping { if (/<\/component>/) skipping = 0; next }
        /<\/application>/ { while ((getline line < font) > 0) print line }
        { print }
    ' "$other")"
    if [[ "$merged" != "$(cat "$other")" ]]; then
        back_up "options/other.xml"
        printf '%s\n' "$merged" > "$other"
    fi
else
    { echo "<application>"; cat "$font"; echo "</application>"; } > "$other"
fi

settings_version > "$installed_version"
rm -f "$pending"

echo "Rider settings installed into $config"
if [[ -d "$backup" ]]; then
    echo "The settings they replaced are in $backup"
fi
