SHELL = cmd

REPO_PATH = D:\SteamLibrary\steamapps\common\REPO
NAME = CommandLine
DLL = bin\Debug\netstandard2.1\$(NAME).dll

All: Compile Push Debug

Compile: $(DLL) 

$(DLL): Plugin.cs Commands.cs Utils.cs Patches.cs $(NAME).csproj
	dotnet build
	copy $(DLL) $(REPO_PATH)\BepInEx\plugins\$(NAME).dll /B
	copy $(DLL) Libraries\$(NAME).dll

Debug: 
	$(REPO_PATH)\REPO.exe

Push:
	cd /D $(REPO_PATH)\BepInEx\plugins &&\
		git add . && \
		git commit -m "Recompiled plugins" &&\
		git push origin main

Clean:
	del $(DLL)
	
.IGNORE: Debug

.PHONY: Compile Debug Push 

