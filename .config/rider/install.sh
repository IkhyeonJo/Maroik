#!/usr/bin/env bash
#
# Copies the Rider settings of the repository into this PC's Rider configuration. Run it once per PC, with Rider closed
# (Git Bash on Windows).
#
# The solution itself carries what Rider reads from a solution: run configurations (.run), required plugins, inspection
# profile and dictionary (.idea), code style and severities (.editorconfig, Maroik.slnx.DotSettings). The keymap, color
# scheme, theme and editor options are IDE-level: Rider reads them only from its configuration folder, whatever solution
# is open, so they need this copy. settings/ mirrors that folder and holds no license, account or path of a PC.

set -euo pipefail

repository_settings="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exported_version="Rider2026.2"
is_windows=false

case "$(uname -s)" in
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

# Rider writes its settings back when it exits, which would undo the copy. The folder's .lock holds its Rider's process
# id, and is left behind after the exit, so what counts is whether that process still runs.
lock_holder_runs() {
    local pid
    [[ -s "$config/.lock" ]] || return 1
    pid="$(cat "$config/.lock")"
    if [[ "$is_windows" == true ]]; then
        # Git Bash's kill knows only its own processes.
        tasklist.exe //FI "PID eq $pid" | grep -qw "$pid"
    else
        kill -0 "$pid" 2>/dev/null
    fi
}

if lock_holder_runs; then
    echo "Close Rider first: it would overwrite these settings when it exits ($config)." >&2
    exit 1
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

echo "Rider settings installed into $config"
if [[ -d "$backup" ]]; then
    echo "The settings they replaced are in $backup"
fi
