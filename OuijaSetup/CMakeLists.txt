cmake_minimum_required(VERSION 3.10)

if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

project(OuijaSetup)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(OuijaSetup OuijaSetup.cpp)

if(WIN32)
	set_target_properties(OuijaSetup PROPERTIES OUTPUT_NAME "Windows_Ollama_Setup")
elseif(APPLE)
	set_target_properties(OuijaSetup PROPERTIES OUTPUT_NAME "MacOS_Ollama_Setup")
else()
	set_target_properties(OuijaSetup PROPERTIES OUTPUT_NAME "Linux_Ollama_Setup")
endif()
