Setup
=====

- Install .NET 5.0
- Run `dotnet tool install refriender -g`

Usage
=====

`refriender [args] filename`

```
  -q, --quiet                   Silence messages
  -v, --verbose                 Verbose messages
  -s, --start-only              Only find starting positions of blocks
  -i, --identify                Look for magic in decompressed blocks
  --read-file                   Read entire file into memory instead of mapping
                                (Warning: ONLY works for files <=2GB)
  -p, --preserve-overlapping    Preserve overlapping blocks
  -e, --extract-to              Path for extraction
  -a, --algorithms              (Default: all) Comma-separated list of
                                algorithms (valid options: all, deflate, zlib,
                                gzip, bzip2, lzw, lz4raw, lz4frame)
  -f, --find-pointers           Comma-separated list of offsets/ranges in which
                                to search for pointers to blocks (e.g. 0,4,8,16
                                or 1-8,32)
  -F, --find-end-pointers       Comma-separated list of offsets/ranges in which
                                to search for pointers to the end of blocks
                                (e.g. 0,4,8,16 or 1-8,32)
  -m, --min-length              (Default: 128) Minimum decompressed block length
                                in bytes
  --help                        Display this help screen.
  filename (pos. 0)             Required. File to scan
```

Refriender can be used to find and extract all the compressed blocks in a file, either with a single algorithm, a set of algorithms, or (the default) all of them. It can also find pointers to those blocks, or to just before/after the block, as is often the case with archive files. Additionally, libmagic can be used to identify the contents of decompressed blocks.
