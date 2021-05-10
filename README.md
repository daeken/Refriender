Setup
=====

- Install .NET 5.0
- Clone the repo

Usage
=====

From the Refriender project subdirectory:

`dotnet run -- [args] filename`

```
  -q, --quiet                   Silence messages

  -v, --verbose                 Verbose messages

  -s, --start-only              Only find starting positions of blocks

  -p, --preserve-overlapping    Preserve overlapping blocks

  -e, --extract-to              Path for extraction

  -a, --algorithms              (Default: all) Comma-separated list of algorithms (valid options: all (!SLOW!), deflate, zlib, gzip,
                                bzip2, lzma, lzma2, lzw)

  -f, --find-pointers           Comma-separated list of offsets/ranges in which to search for pointers to blocks (e.g. 0,4,8,16 or
                                1-8,32)

  -m, --min-length              (Default: 1) Minimum decompressed block length

  --help                        Display this help screen.

  --version                     Display version information.

  filename (pos. 0)             Required. File to scan
```

Refriender can be used to find and extract all the compressed blocks in a file, either with a single algorithm, a set of algorithms, or (the very slow default) all of them. It can also find pointers to those blocks, or to just before the block, as is often the case with archive files.
