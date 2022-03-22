# Blazor-With-In-Maui-and-Web
sample project that run Blazor library in web and Maui project
Installing MAUI
Once the prerequisites are installed, the last step is installing the .NET MAUI workloads. This process is straightforward, and it only takes a single command to run in a command prompt or terminal.

You can also verify your development environment and install any missing components by using the maui-check utility. We will cover it later in this section.

# Installing the .NET MAUI workloads

In a command prompt or terminal, run the following command to install the .NET MAUI workloads

### dotnet workload install maui

If you have trouble with the dotnet command not being found, then open the Developer Command Prompt for VS 2022 Preview and try it again.

Verifying and installing missing components maui-check utility
The maui-check utility is a command line tool that verifies your development environment and installs any missing components.

Run the following .NET Command Line Interface (.NET CLI) command to install the maui-check utility:

### dotnet tool install -g redth.net.MAUI.check

If you already have a previous version of maui-check installed, update it to the latest version with the following .NET CLI command:

### dotnet tool update -g redth.net.MAUI.check

Finally, run the following command to check your development environment:

### maui-check

If any tools and SDKs required by .NET MAUI are missing, maui-check will prompt you to install them.
![image](https://user-images.githubusercontent.com/8823894/159461512-0b5fc665-a735-48ef-89db-ed6ed3735448.png)
Figure 7 – Message from maui-check with prompts to install missing dependencies

Once you install the missing components, run maui-check again to ensure that your environment has the latest tools and SDKs required by .NET MAUI.

You should see the message Congratulations, everything looks great! when every dependency has been installed.
![image](https://user-images.githubusercontent.com/8823894/159461728-69fc5e36-420f-439a-bac5-fef82c5427ef.png)
Figure 8 – Message from maui-check confirming that workloads are all installed
