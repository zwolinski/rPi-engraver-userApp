# rPi-engraver-userApp

Raspberry Pi-Controlled Laser Engraver
## About the project
The project consists of two separate applications: a driver (in Python) launched on Raspberry Pi and an application for Windows (in C# using WPF).
### User application
User application allows to convert the input colored image to grayscale, and then it performs binarization and generates control instructions (similar to G code).
### Driver 
The driver, based on received data, controls the motors, laser and power supply.
### Device
Third part of the project is the device - a small-sized laser engraver.
