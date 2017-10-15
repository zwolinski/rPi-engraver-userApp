# rPi-engraver-userApp

**_Note:_** *the project is still in beta version.*

Raspberry Pi-Controlled Laser Engraver
## Project overview
The project consists of two separate applications: a driver (in Python) launched on Raspberry Pi and an application for Windows (in C# using WPF).
### User application
User application allows to convert the input colored image to grayscale, and then it performs binarization and generates control instructions (similar to G code). You can find a whole project in this repo.
### Driver 
The driver, based on received data, controls the motors, laser and power supply. You can find the code here: https://github.com/zwolinski/rPi-engraver
### Device
Third part of the project is the device - a small-sized laser engraver.
