#!/bin/sh
# Runs the client-script toolchain inside the Node container that Maroik.Website.csproj starts when the
# machine has no node_modules here (ClientScriptsInDocker; Rider / Visual Studio "Fast Mode" and a plain
# Debug `dotnet build` without Node). Usage, from the project folder mounted at /w/app:
#     sh client-scripts-in-docker.sh build:debug   # npm run build:debug -> wwwroot/**/custom/**/site.js(.map)
#     sh client-scripts-in-docker.sh test          # vitest run against the compiled site.js
#
# The packages live in the named volume mounted at /w (/w/node_modules), not in the project folder: the
# host keeps no Linux node_modules (which would also switch the csproj back to the host's npm), and Node,
# tsc (@types) and vitest find the packages from /w/app by walking up to /w/node_modules. `npm ci` runs
# only when package-lock.json differs from the copy stored after the last successful install.
set -eu

cd /w
if ! cmp -s app/package-lock.json package-lock.json || [ ! -f node_modules/.maroik-installed ]; then
    echo "npm ci: installing the client-script toolchain into the Docker volume"
    rm -f node_modules/.maroik-installed
    cp app/package.json app/package-lock.json app/.npmrc .
    npm ci --no-audit --no-fund
    touch node_modules/.maroik-installed
fi
export PATH="/w/node_modules/.bin:$PATH"

cd /w/app
case "$1" in
    test) vitest run ;;
    *) npm run "$1" ;;
esac

# The container runs as root; hand what it wrote back to the owner of the project folder, so the host can
# rebuild or delete it (a no-op on Docker Desktop, where the bind mount maps owners itself).
find wwwroot -path '*/custom/*' \( -name '*.js' -o -name '*.js.map' \) -exec chown "$(stat -c %u:%g /w/app)" {} +
