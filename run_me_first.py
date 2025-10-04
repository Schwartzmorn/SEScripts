#! python3

# run this script (no arguments) to recreate the sln, shproj, ini... files

import os
import uuid

PROJITEMS_TEMPLATE = """<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <MSBuildAllProjects Condition="'$(MSBuildVersion)' == '' Or '$(MSBuildVersion)' &lt; '16.0'">$(MSBuildAllProjects);$(MSBuildThisFileFullPath)</MSBuildAllProjects>
    <HasSharedItems>true</HasSharedItems>
    <SharedGUID>8a3cdcc5-4b55-4d87-a415-698a0e1ff06f</SharedGUID>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)\\*.cs" />
  </ItemGroup>
</Project>
"""

MDKINI_TEMPLATE = """[mdk]
type=programmableblock
trace=off
minify=lite
ignores=obj/**/*,MDK/**/*,**/*.debug.cs
output=auto
binarypath=auto
"""

SLN_TEMPLATE = """
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.5.2.0
MinimumVisualStudioVersion = 10.0.40219.1
{projects}
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
{project_configurations}
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {{BE490A9C-7324-4360-B400-8E064DC34F5F}}
	EndGlobalSection
EndGlobal
"""

SLN_PROJECT_TEMPLATE = 'Project("{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}") = "{mixin_name}", "{relative_path}", "{{{project_guid}}}"\nEndProject'

SLN_PROJECT_CONFIGURATION_TEMPLATE = '\t\t{{{project_guid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\n\t\t{{{project_guid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU'

def fix_projitems(mixin_folder: str, mixin_name: str) -> None:
  for file in os.listdir(mixin_folder):
    if file.endswith(".projitems"):
      os.remove(os.path.join(mixin_folder, file))

  with open(os.path.join(mixin_folder, f"{mixin_name}.projitems"), "w", encoding="utf-8") as f:
    f.write(PROJITEMS_TEMPLATE)


def fix_mixin(mixin_folder: str) -> dict[str, str]:
  mixin_name = os.path.basename(mixin_folder)
  fix_projitems(mixin_folder, mixin_name)


def fix_mixins(mixins_folder: str):
  for folder in os.listdir(mixins_folder):
    mixin_folder = os.path.join(mixins_folder, folder)
    if (os.path.isdir(mixin_folder)):
      fix_mixin(mixin_folder)


def fix_script(script_folder: str, fix_mdk: bool):
  # remove any existing .mdk.ini files
  for file in os.listdir(script_folder):
    if file.endswith(".mdk.ini"):
      os.remove(os.path.join(script_folder, file))
  if fix_mdk:
    mdk_ini_path = os.path.join(script_folder, f"{os.path.basename(script_folder)}.mdk.ini")
    with open(mdk_ini_path, "w", encoding="utf-8") as f:
      f.write(MDKINI_TEMPLATE)
  # make sure the csproj file is named correctly
  csproj_path = os.path.join(script_folder, f"{os.path.basename(script_folder)}.csproj")
  for file in os.listdir(script_folder):
    if file.endswith(".csproj") and file != f"{os.path.basename(script_folder)}.csproj":
      os.rename(os.path.join(script_folder, file), csproj_path)


def fix_scripts(scripts_folder: str, fix_mdk: bool) -> list[str]:
  scripts = []
  for folder in os.listdir(scripts_folder):
    script_folder = os.path.join(scripts_folder, folder)
    if (os.path.isdir(script_folder)):
      fix_script(script_folder, fix_mdk)
      scripts.append(folder)
  return scripts


def update_project(name: str, path: str, projects: list[str], project_configurations: list[str]) -> None:
  project_guid = str(uuid.uuid4()).upper()
  relative_path = os.path.join(path, name, f"{name}.csproj").replace('/', '\\')
  projects.append(SLN_PROJECT_TEMPLATE.format(mixin_name=name, relative_path=relative_path, project_guid=project_guid))
  project_configurations.append(SLN_PROJECT_CONFIGURATION_TEMPLATE.format(project_guid=project_guid))


def generate_sln(scripts: list[str], tests: list[str]) -> None:
  projects = []
  project_configurations = []
  for script in scripts:
    update_project(script, 'Scripts', projects, project_configurations)
  for test in tests:
    update_project(test, 'Tests', projects, project_configurations)

  projects_str = "\n".join(projects)
  project_configurations_str = "\n".join(project_configurations)
  with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), './OmniOS.sln'), "w", encoding="utf-8") as f:
    f.write(SLN_TEMPLATE.format(projects=projects_str, project_configurations=project_configurations_str))
  

if __name__ == "__main__":
  mixins_folder = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), './Mixins'))
  scripts_folder = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), './Scripts'))
  tests_folder = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), './Tests'))

  fix_mixins(mixins_folder)
  scripts = fix_scripts(scripts_folder, True)
  tests = fix_scripts(tests_folder, False)

  generate_sln(scripts, tests)
