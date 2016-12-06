// This file consists of 4 parts:
// 1. Central framework
// 2. Chassis/manuvering/sensor control
// 3. Shooter - Pitcher type
// 4. Shooter - Pneumatic Cannon type

// P.S. Yes I used ASCII art for section headers...

//  ______________________
// /                      \
//(  Variable Definitions  )
// \______________________/

// --<Chassis Pins>--
const int pinDriveLeft = 0;
const int pinDriveRight = 0;
const int pinEncoderLeft = 0;                         // Analog input
const int pinEncoderRight = 0;                        // Analog input
const int[3] pinIR = { 0, 0, 0 };                     // Line-following IR sensors (analog inputs)
const int pinReload = 0;                              // Pressing this pin resets the ammunition count to maxAmmo
const int pinIndicator = 13;                          // Indicator light

// --<Interconnect Pins>--
const int iSelect = 0;
const int iGPIO0 = 0;
const int iPWM0 = 0;
const int iPWM1 = 0;

// --<Default Values>--
const int maxAmmo = 5;
const int selectPitcher = 0;                          // When iSelect is low, this shooter is installed.
const int selectCannon = 1;                           // Otherwise, this one is installed
const char cmdEnd = '\n';

// --<Variable Variables ;) >--
int ammoRemaining = maxAmmo;
bool isCannon = false;
double[] currentPos = { 0, 0 };                       // Current relative position of bot
double[] requestedMotorSpeeds = { 0, 0 };
String functionRunning = "";
String commandString = "";
//ShooterPitcher shooterPitcher(iGPIO0, iPWM0, iPWM1);
//ShooterCannon shooterCannon(iGPIO0, iPWM0, iPWM1);

//  __________________________
// /                          \
//(  Setup and Initialization  )
// \__________________________/

void setup() {
  Serial.begin(9600);
  pinMode(pinDriveLeft, OUTPUT);
  pinMode(pinDriveRight, OUTPUT);
  pinMode(pinReload, INPUT);
  pinMode(pinIndicator, OUTPUT);
  pinMode(iSelect, INPUT);
  isCannon = digitalRead(iSelect) == HIGH;
  if (isCannon) {
    // shooterCannon.setup();
  } else {
    // shooterPitcher.setup();
  }
}

//  ___________
// /           \
//(  Main Loop  )
// \___________/

void loop() {
  if (isCannon != (digitalRead(iSelect) == HIGH)) {
    isCannon = digitalRead(iSelect) == HIGH;
    if (isCannon) {
      // shooterPitcher.reset();
      // shooterCannon.setup();
    } else {
      // shooterCannon.reset();
      // shooterPitcher.setup();
    }
  } 
}

//  _____________
// /             \
//(  Serial Loop  )
// \_____________/

void serialEvent() {
  char newChar = null;
  while (Serial.available() > 0) {
    newChar = Serial.read();
    if (newChar == cmdEnd) { 
      handleCommandString();
      commandString = "";
    }
    else {
      commandString += newChar;
    }
  }
}

void handleCommandString() {
  // sets function to perform
  // also delegate quick functions?
   // Commands:
  // SETMOTORS X,Y[,T]                                // Set motors to speeds [X, Y], optionally for T milliseconds
  // FIRE
  // SETSTART
  // GOTO START|RANGE
  // RECORD START|STOP|GO                         
  // STOP
  // FOLLOW
  Serial.print("OK 0,0 0,0 0,0 0 0000 5 N");
}
