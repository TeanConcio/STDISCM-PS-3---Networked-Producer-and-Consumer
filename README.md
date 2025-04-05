# STDISCM PS 3 - Networked Producer and Consumer

Github Link: https://github.com/TeanConcio/STDISCM-PS-3---Networked-Producer-and-Consumer [https://github.com/TeanConcio/STDISCM-PS-3---Networked-Producer-and-Consumer]

## Instructions to Run
0. Have Visual Studio installed with the .NET desktop development bundle.
1. Double click the "STDISCM PS 3 - Networked Producer and Consumer.sln" Visual Studio Solution File to open project in Visual Studio.
2. On the Solution Explorer side bar, double click on the "config.txt" of the projects to open it.
3. Change the configurations as needed.
4. In the producer project, browse to the "video_folders" folder, add the folders with the videos you want to use, and name them accordingly.
	- The folders of the threads should be named "video_folder_<thread_number>", starting with "video_folder_0".
5. On the tool bar at the top, run the part of the project you want to run.
	- If you want to run the producer, run the "Producer" project.
	- If you want to run the consumer, run the "Consumer" project.
	- If you want to run both in the same machine, run the "Full Proj Start" project.
6. As the consumer scans for duplicates of existing videos upon startup, don't forget to navigate to its "downloaded_videos" folder and reset the videos.
7. The slides be found in the base folder, with it being labeled accordingly.
