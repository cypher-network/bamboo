# cypher bamboo wallet

[![Build wallet](https://github.com/cypher-network/bamboo/workflows/build%20wallet/badge.svg)](https://github.com/cypher-network/bamboo/commits/master/)
[![GitHub release](https://img.shields.io/github/release/cypher-network/bamboo.svg)](https://GitHub.com/cypher-network/bamboo/releases/)


| OS             | Version      | Architecture | Supported           |
|----------------|--------------|--------------|---------------------|
| Ubuntu         | 21.10,22.04  | x64          | :heavy_check_mark:  |
| CentOS Stream  | 8,9          | x64          | :heavy_check_mark:  |
| Windows        | 10,11        | x64          | :heavy_check_mark:  |
| macOS          | 11,12        | x64          | :heavy_check_mark:  |
| Raspberry Pi 4 | 10 (buster)  | x64          | :heavy_check_mark:  |

> Some unknown operating systems are still being tested. 
> If you are technical or would like to get your hands dirty, please go ahead and install the wallet. It won't bite :yum:


## Installation
### .Net 6

Downloads for .Net
https://dotnet.microsoft.com/en-us/download

### Linux and macOS

For quick installation on Linux and macOS, execute the following command:

```shell
bash <(curl -sSL https://raw.githubusercontent.com/cypher-network/bamboo/master/install/install.sh)
```

The following parameters can be supplied:

`--help`
Display help
  
`--uninstall`
Uninstall wallet

  
For example:

```shell
bash <(curl -sSL https://raw.githubusercontent.com/cypher-network/bamboo/master/install/install.sh) --uninstall
```

> In some cases, `macOS` users might need to install gmp. The secp256k1 library depends on gmp (arbitrary precision arithmetic).

For quick installation execute the following command:

`brew install gmp`

If you don't have homebrew installed:

### Installing Homebrew macOS

Users running Catalina, Mojave, or Big Sur, execute the following command if you don't have homebrew installed:

```shell
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"
````

### Microsoft Windows

For quick installation on Windows
https://github.com/cypher-network/bamboo/releases

Once installed open Powershell/CMD in Administrative mode then type `clibamwallet --configure`

## Safety

This software is using cryptography that has not been formally audited.
While we do our best to make it safe, it is up to the user to evaluate whether or not it is safe to use for their purposes.

## Contribution

Thank you for considering to help out with the source code!

If you'd like to contribute to Bamboo (CLi wallet), please know we're currently only accepting issues. If you wish to submit more
complex changes though, please check up with the core devs first on [Discord Channel] (https://discord.gg/6DT3yFhXCB) 
to ensure the changes get some early feedback which can make both your efforts much lighter as well as review quick and simple.
