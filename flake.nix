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
          # core = pkgs.buildDotnetModule {
          #   pname = "dmq-maker-core";
          #   version = "1.1.0";
          #
          #   src = "${self}/DMQCore";
          #   nugetDeps = builtins.fromJSON (builtins.readFile "Lambda/deps.json");
          #   projectFile = "DMQCore/DMQCore.csproj";
          #
          #   nativeBuildInputs = with pkgs; [
          #     dotnet-sdk
          #   ];
          # };
          lambda = pkgs.buildDotnetModule {
            pname = "dmq-maker-lambda";
            version = "1.0.0";

            src = "${self}";
            nugetDeps = "${self}/Lambda/deps.json";
            projectFile = "${self}/Lambda/Lambda.csproj";

            buildInputs = with pkgs; [
              dotnet-sdk
            ];

            # buildPhase = ''
            #   dotnet build Lambda/Lambda.csproj -c Release --output $out
          };
          # doesnt work
          generate-deps = pkgs.writeShellScriptBin "generateDeps" ''
            git_root=$(git rev-parse --show-toplevel)
            dotnet restore $git_root/Lambda/Lambda.csproj
            ${pkgs.nuget-to-json}/bin/nuget-to-json $git_root/Lambda/packages > $git_root/Lambda/deps.json
          '';

        };
      }
    );
}

# Here are build commands to make zip for lambda:
# dotnet build Lambda/Lambda.csproj -c Release
# mkdir -p bin && zip -r -j bin/package.zip Lambda/bin/Release/net8.0/ 
