# What are Nix Flakes https://www.tweag.io/blog/2020-05-25-flakes/
{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; config.allowUnfree = true; };
      in {
        devShell = pkgs.mkShell {
          buildInputs = with pkgs; [
            dotnet-sdk
            zip
          ];
        };
        packages.default = pkgs.stdenv.mkDerivation {
          pname = "dmq-maker-lambda";
          version = "1.0.0";

          src = "${self}";

          nativeBuildInputs = with pkgs; [
            dotnet-sdk
          ];

          buildPhase = ''
            dotnet build Lambda/Lambda.csproj -c Release --output $out
          '';
          __noChroot = true;
          # NOTE: nix seems to be unable to build dotnet apps. It cannot fetch nugets. The noChroot should do something about it, but nix complains that sandbox must be false. `nix build --option sandbox false` - doesnt do anything (maybe must be run as root user)
          # So I decided to build manually
        };
      }
    );
}

# Here are build commands to make zip for lambda:
# dotnet build Lambda/Lambda.csproj -c Release
# mkdir -p bin && zip -r -j bin/package.zip Lambda/bin/Release/net8.0/ 
