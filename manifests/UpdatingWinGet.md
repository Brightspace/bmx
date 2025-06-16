# Updating Winget Manifest

This manifest was created with [winget-create](https://github.com/microsoft/winget-create).

### Install winget-create

`winget install wingetcreate`

### Run the update command

`wingetcreate update D2L.BMX --urls <x64-url> <arm64-url> --version X.X.X --submit`

The `--submit` flag will automatically open a Pull Request on the winget-pkgs repo. A new folder will be created in the manifests file. Set your current working directory to that.

### Validate the manifest

`winget validate --manifest ./`

### Install BMX and try running it

`winget install --manifest ./`
`bmx --version`

### Update the Pull Request

Tick off the checkboxes for having signed the CLA and all of the ones under the 'Manifests' section

Once approved, it will be automatically merged by a maintainter of the repo.
