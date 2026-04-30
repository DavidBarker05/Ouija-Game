import platform
import subprocess

current_os = platform.system()

if current_os == "Windows":
    try:
        subprocess.run("Windows_Ollama_Setup.exe", Check = True)
    except FileNotFoundError:
        print("The executable for Windows could not be found.")
        print("An executable can be built for Windows by using CMake and your c++ compiler of choice in the OuijaSetup folder.")
elif current_os == "Darwin": # Macos
    try:
        subprocess.run("MacOS_Ollama_Setup.app", Check = True)
    except FileNotFoundError:
        print("The executable for MacOS could not be found.")
        print("I was unable to build for MacOS because Apple requires me to own a Mac to build for it, or illegally download a VM for MacOS which I didn't feel like doing.")
        print("An executable can be built for MacOS by using CMake and your c++ compiler of choice in the OuijaSetup folder.")
else: # Linux
    try:
        subprocess.run("Linux_Ollama_Setup", Check = True)
    except FileNotFoundError:
        print("The executable for Linux could not be found.")
        print("An executable can be built for Linux by using CMake and your c++ compiler of choice in the OuijaSetup folder.")