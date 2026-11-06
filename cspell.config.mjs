// Spell check, run by `dotnet test` (SpellingDictionaryArchitectureTests.TheRepository_HasNoSpellingErrors, which pins the
// cspell version and passes the files git does not ignore).
// American English, like Rider's default. Project words live in one place, Rider's project dictionary, which "Save to
// project-level dictionary" writes; this file is JavaScript instead of YAML so that cspell can read them from there.
import { readFileSync } from "node:fs";

const riderDictionary = new URL("./.idea/.idea.Maroik/.idea/dictionaries/project.xml", import.meta.url);

export default {
  version: "0.2",
  language: "en-US",
  useGitignore: true,
  words: [...readFileSync(riderDictionary, "utf8").matchAll(/<w>(.*?)<\/w>/g)].map((match) => match[1]),
  dictionaries: [
    "csharp",
    "dotnet",
    "typescript",
    "node",
    "npm",
    "html",
    "css",
    "bash",
    "docker",
    "sql",
    "softwareTerms",
    "companies",
    "misc",
    "fullstack",
  ],
  files: [
    "**/*.{cs,ts,mts,mjs,cshtml,md,yml,yaml,sh,csproj,props,slnx,resx,json,globalconfig,nuspec}",
    "**/Dockerfile",
    "Maroik.Caddy/Caddyfile",
    "Maroik.Website/wwwroot/*/custom/**/*.css",
  ],
  ignorePaths: [
    "**/bin/**",
    "**/obj/**",
    "**/node_modules/**",
    "**/package-lock.json",
    "Maroik.Website/wwwroot/**/*.js",
    "Maroik.Website/wwwroot/**/lib/**",
    "Maroik.Website/wwwroot/**/plugins/**",
    "Maroik.Website/wwwroot/**/dist/**",
    // EF Core scaffold output (CLAUDE.md: left out of repo-wide comment / style passes) and the seed data.
    "Maroik.Core.PostgreSQL/Contexts/**",
    "Maroik.Core.PostgreSQL/Models/**",
    "Maroik.DB/**",
  ],
  ignoreRegExpList: [
    "Urls",
    "Email",
    "HexValues",
    "Base64",
    // Text in other scripts (Korean, Cyrillic, Greek, full-width Latin) is test data, not English.
    "/[^\\x00-\\x7F]+/g",
  ],
};
