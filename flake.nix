{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; config.allowUnfree = true; };
      in rec {
        devShell = pkgs.mkShell {
          buildInputs = with pkgs; [
            dotnet-sdk
            zip
            nuget-to-json
          ];
        };
        packages = {
          default = packages.lambda;
          lambda = pkgs.buildDotnetModule {
            pname = "dmq-maker-lambda";
            version = "1.0.0";

            src = ./.;
            nugetDeps = ./Lambda/deps.json;
            projectFile = "./Lambda/Lambda.csproj";

            buildType = "Release";
            selfContainedBuild = false;
          };
          generate-deps = pkgs.writeShellScriptBin "generateDeps" ''
            git_root=$(git rev-parse --show-toplevel)
            dotnet restore --packages $git_root/Lambda/packages $git_root/Lambda/Lambda.csproj
            ${pkgs.nuget-to-json}/bin/nuget-to-json $git_root/Lambda/packages > $git_root/Lambda/deps.json
          '';
        };
      }
    );
}

# Here are build commands to make zip for lambda:
# dotnet publish Lambda/Lambda.csproj -c Release
# mkdir -p bin && zip -r -j bin/package.zip Lambda/bin/Release/net8.0/linux-x64/publish/
