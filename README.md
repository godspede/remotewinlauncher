# remotewinlauncher

WIP.

Use case: you have a Windows program that you want to kick off from the web on your logged in user session, so that you can see an actual UI. This is currently coded up to prevent duplicate windows by checking to see if a Title exists on a running process.

Recommend that you set it up in Task Scheduler to trigger periodically. Use the -h parameter to hide the UI (some critical events can make it up with a warning message).

Here's an example that will launch program from a batch file (in this case, a stable diffusion webui) when visiting https://localhost:5002/launch/secret : 
	remotewinlauncher.exe -s secret -p "C:\repo\launch_stable-diffusion-webui.bat" -t "stable diffusion webui" -h

In that example, the .bat is using this command to ensure that the window's title is set correctly: `TITLE stable diffusion webui`