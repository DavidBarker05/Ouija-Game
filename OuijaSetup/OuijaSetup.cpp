#if defined(_WIN32) || defined(_WIN64) // Windows
#define WINDOWS 1
#endif

#include <cstdlib>
#include <iostream>
#include <string>
#include <fstream>
#include <sstream>
#if WINDOWS
#include <windows.h>
#define PATH_MAX MAX_PATH
#define SEPARATOR '\\'
#if UNICODE
#include <codecvt>
#define buffer_t wchar_t
#define string_t std::wstring
#define TO_STRING(wstring) std::wstring_convert<std::codecvt_utf8<wchar_t>>().to_bytes(wstring)
#else
#define buffer_t char
#define string_t std::string
#define TO_STRING(string) string
#endif
#else // Linux, MacOS, etc.
#include <unistd.h>
#include <limits.h>
#define SEPARATOR '/'
#define buffer_t char
#define string_t std::string
#define TO_STRING(string) string
#endif


#if WINDOWS
#define OLLAMA_INSTALL_COMMAND "powershell.exe -ExecutionPolicy Bypass -Command \"irm https://ollama.com/install.ps1 | iex\""
#else // Linux, MacOS, etc.
#define OLLAMA_INSTALL_COMMAND "curl -fsSL https://ollama.com/install.sh | sh"
#endif

#define CSTRING const char*

bool ExecuteCommand(CSTRING cmd)
{
	return std::system(cmd) == EXIT_SUCCESS;
}

std::string GetCurrentFolder()
{
	buffer_t pathBuffer[PATH_MAX];
	std::string output;
#if WINDOWS
	GetModuleFileName(nullptr, pathBuffer, PATH_MAX);
#else
	ssize_t len = readlink("/proc/self/exe", pathBuffer, sizeof(pathBuffer) - 1);
	if (len != -1) pathBuffer[len] = '\0';
#endif
	output = TO_STRING(string_t(pathBuffer));
	std::size_t lastPos = output.find_last_of(SEPARATOR);
	output = output.substr(0, lastPos + 1);
	if (output.back() != SEPARATOR) output += SEPARATOR;
	return output;
}

std::string ReadFile(CSTRING fileName)
{
	std::string folder = GetCurrentFolder();
	std::string fileLocation = folder + fileName;
	std::ifstream file { fileLocation };
	std::stringstream buffer;
	buffer << file.rdbuf();
	return buffer.str();
}

bool IsOllamaInstalled()
{
	return ExecuteCommand("ollama -v");
}

void InstallOllama()
{ 
	ExecuteCommand(OLLAMA_INSTALL_COMMAND);
}

std::string GetAIModel(CSTRING fileName)
{
	std::string aiFile = ReadFile(fileName);
	std::stringstream ss(aiFile);
	std::string model;
	ss >> model;
	return model;
}

bool IsAIModelInstalled(std::string model)
{
	std::string cmdStr = std::string("ollama show ") + model;
	return ExecuteCommand(cmdStr.c_str());
}

void InstallAIModel(std::string model)
{
	std::string cmdStr = std::string("ollama pull ") + model;
	ExecuteCommand(cmdStr.c_str());
}

int main()
{
	if (!IsOllamaInstalled()) InstallOllama();
	std::string storyModel = GetAIModel("StoryModel.txt");
	if (!storyModel.empty() && !IsAIModelInstalled(storyModel)) InstallAIModel(storyModel);
	std::string ouijaModel = GetAIModel("OuijaModel.txt");
	if (!ouijaModel.empty() && !IsAIModelInstalled(ouijaModel)) InstallAIModel(ouijaModel);
	std::cout << "Setup complete. Thank you for your patience!" << std::endl;
	std::cout << "Press ENTER to exit...";
	std::cin.get();
}
