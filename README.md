# GammaCopy

Reconstitute directory structure and files by use of a metadata cache and templates.  Currently only supports SmokeMonster DataBase (or SMDB) templates.


License is Apache 2.0 because of [Crc32.cs](https://github.com/damieng/DamienGKit/blob/master/CSharp/DamienG.Library/Security/Cryptography/Crc32.cs)

[origin of SMDBs](https://github.com/SmokeMonsterPacks/EverDrive-Packs-Lists-Database)

Template files allow one to share/archive file organization and identities by use of cryptographic hashes.  Templates work well for preserving metadata, which can be used to reconstitute file/folder structures.  Templates provide a way to comment on and preserve the hierarchy of sequences of bytes.

GammaCopy currently supports only the SMDB format.  Plans include adding support for additional template formats.

***
## Disclaimer
**You are responsible for your own actions.** If you mess something up or loose data while using this software, it's your fault, and your fault only.

- *I am in no way affiliated with SmokeMonster, nor is this project endorsed by SmokeMonster.*

***

## Command line

| verb  | description                                                                                       |
|-------|---------------------------------------------------------------------------------------------------|
| parse | Generate a template file.                                                                         |
| index | Gather metadata for the source locations and save it in the metadata cache.                       |
| build | Check coverage or reconstitute directory structure and files based on a template file.            |

### parse options

`-p, --parse-folder`                **Required**. Folders to be parsed.

`-w, --parsed-output-destination`    **Required**. Write parsed output to this file.

`-r, --global-prepend`               prepend to all paths.

`-t, --prepend-last-folder`          prepend the rightmost folder of the current parse folder to paths.

### index options

`-s, --source`        **Required**. Source locations to index or refresh.

`-e, --onebyone`    Gather metadata sequentially.

`-x, --disable-archive-traversal`    Do not traverse archive files.

### build options

`-d, --database` **Required**. template files to use.

`-o, --output` Path to the output folder.

`-n, --containers` Write output to `<outputpath>/<templatefilename>/`

`-f, --coverage-hybrid-to-file` Gather entries not in output location or metadata cache. Output coverage information to file.

`-c, --coverage-hybrid-to-stdout` Gather entries not in output location or metadata cache. Output coverage summary to console.

`-u, --coverage-existant-to-file` Gather entries not in output location. Output coverage information to file.

`-v, --coverage-existant-to-stdout` Gather entries not in output location. Output coverage summary to console.

`-h, --coverage-meta-to-file` Gather entries not in metadata cache. Output coverage information to file.

`-b, --coverage-meta-to-stdout` Gather entries not in metadata cache. Output coverage summary to console.

`-j, --stdout-coverage-full` When outputting coverage to console, DO NOT omit the missing entry list.

`-k, --delete-extras` Delete extra files found in the output path but not in the template file.

`-l, --delete-empty-folders` Delete empty folders found in the output path.

`-g, --go` Go ahead with the build process and output the data to the output folder.

## Examples

`>GammaCopy build -d "Y:\mySMDB.txt" -c -f -g -o "Y:\nes"`

`>GammaCopy index -s "Y:\pilesofarchives\"`

`>GammaCopy parse -p "Y:\myperfectset" -w "Y:\perfectset.txt" -r "perfecto"`
