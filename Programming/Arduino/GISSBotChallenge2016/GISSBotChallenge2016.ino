#include <ShooterPitcher.h>
#include <ShooterCannon.h>

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
const int pinDriveLeft = 10;
const int pinDriveRight = 11;
const int pinEncoderLeft = 0;                         // Analog input
const int pinEncoderRight = 0;                        // Analog input
const int pinIR[3] = { 0, 1, 2 };                     // Line-following IR sensors (analog inputs)
const int pinReload = 12;                              // Pressing this pin resets the ammunition count to maxAmmo
const int pinIndicator = 13;                          // Indicator light

// --<Interconnect Pins>--
const int iSelect = 4;                                // Selector pin on the connector (pin 2) - tied to ground = ShooterCannon
const int iGPIO0 = 7;                                 // GPIO pin on the connector (pin 3)
const int iPWM0 = 5;                                  // PWM pin 0 on the connector (pin 4)
const int iPWM1 = 6;                                  // PWM pin 1 on the connector (pin 5)

// --<Default Values>--
const int maxAmmo = 5;
const int selectPitcher = 0;                          // When iSelect is high, this shooter is installed.
const int selectCannon = 1;                           // Otherwise, this one is installed
const char cmdEnd = '\n';

// --<Variable Variables ;) >--
int ammoRemaining = maxAmmo;
bool isCannon = false;
double currentPos[] = { 0, 0 };                       // Current relative position of bot, roughly in meters
double currentVector[] = { 0, 0 };                    // Current relative direction of bot, in degrees
double currentMotorSpeeds[] = { 0, 0 };               // Current motor duty percentages
double requestedMotorSpeeds[] = { 0, 0 };             // Requested motor duty percentages
String functionRunning = "";                          // Name of the currently running auto function
String commandString = "";                            // Serial input/command buffer

// --<Shooters>--
ShooterPitcher shooterPitcher(iGPIO0, iPWM0, iPWM1);
ShooterCannon shooterCannon(iGPIO0, iPWM0, iPWM1);

// --<Autonomous Function Specific Variables>--
double recording[512][2] = {};                        // New entries will be added as coordinates once they are different enough while recording
int playbackIndex = 0;

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
  pinMode(iSelect, INPUT_PULLUP);                     // Tied to ground, this input will be LOW, otherwise HIGH
  isCannon = digitalRead(iSelect) == LOW;             // Check which shooter is installed
  if (isCannon) {
    shooterCannon.setup();
  } else {
    shooterPitcher.setup();
  }
}

//  ___________
// /           \
//(  Main Loop  )
// \___________/

void loop() {
  if (isCannon != (digitalRead(iSelect) == LOW)) {    // Make sure the correct shooter is still installed, otherwise initialize the new one
    isCannon = digitalRead(iSelect) == LOW;
    if (isCannon) {
      shooterPitcher.reset();
      shooterCannon.setup();
    } else {
      shooterCannon.reset();
      shooterPitcher.setup();
    }
  }
  functionLoopBroker();                               // If there is a running function, call its loop
  sendStatus();                                       // Send the current status to the RPi
}

//  _____________
// /             \
//(  Serial Loop  )
// \_____________/

void serialEvent() {
  char newChar;
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
  // SETMOTORS X,Y[,T]                                    // Set motors to speeds [X, Y], optionally for T milliseconds
  // FIRE
  // SETSTART
  // GOTO START|RANGE
  // RECORD START|STOP|GO                         
  // STOP
  // FOLLOW
  if (commandString == "STOP") {
    if (functionRunning != "") {
      autonomousStop();
    }
    functionRunning = "";
    requestedMotorSpeeds[0] = 0;requestedMotorSpeeds[1] = 0;
  } else if (functionRunning == "") {
    if (commandString.startsWith("SETMOTOR")) {
      // Count commas: if 1, parse for X and Y; if 2, also parse for T
    } else if (commandString == "FIRE" and ammoRemaining > 0) {
      if (isCannon) {
        shooterCannon.prepare();
        shooterCannon.fire();
      } else {
        shooterPitcher.prepare();
        shooterPitcher.fire();
      }
      ammoRemaining--;
    } else if (commandString == "SETSTART") {
      currentPos[0] = 0;currentPos[1] = 0;
      currentVector[0] = 0;currentVector[1] = 0;
    } else if (commandString == "GOTO START") {
      autonomousStart_GotoStart();
      functionRunning = "GOTO START";
    } else if (commandString == "GOTO RANGE") {
      autonomousStart_GotoRange();
      functionRunning = "GOTO RANGE";
    } else if (commandString.startsWith("RECORD")) {
      if (commandString.endsWith("START")) {
        autonomousStart_Record();
        functionRunning = "RECORD";
      } else if (commandString.endsWith("GO")) {
        autonomousStart_RecordGo();
        functionRunning = "RECORD GO";
      }
    } else if (commandString == "FOLLOW") {
      autonomousStart_Follow();
      functionRunning = "FOLLOW";
    }
  }
}

//  ______________
// /              \
//(  Loop helpers  )
// \______________/

void functionLoopBroker() {
  if (functionRunning == "GOTO START") {
    autonomousLoop_GotoStart();
  } else if (functionRunning == "GOTO RANGE") {
    autonomousLoop_GotoRange();
  } else if (functionRunning == "RECORD") {
    autonomousLoop_Record();
  } else if (functionRunning == "RECORD GO") {
    autonomousLoop_RecordGo();
  } else if (functionRunning == "FOLLOW") {
    autonomousLoop_Follow();
  } else {
    functionRunning = "";
  }
}

void sendStatus() {
  // Send a status string in the format:
  // [STATUS] [MOTORS] [VECTOR] [POSITION] [SEL] [IR] [AMMO] [RECSTATUS]
  Serial.print((functionRunning == "") ? "OK " : "WAIT "); // [STATUS]
  Serial.print(String(currentMotorSpeeds[0])+","+String(currentMotorSpeeds[1])+" "); // [MOTORS]
  Serial.print(String(currentVector[0])+","+String(currentVector[1])+" "); // [VECTOR]
  Serial.print(String(currentPos[0])+","+String(currentPos[1])+" "); // [POSITION]
  Serial.print((isCannon) ? "1 " : "0 "); // [SEL]
  Serial.print("0000 "); // [IR]
  Serial.print(String(ammoRemaining)+" "); // [AMMO]
  Serial.print((functionRunning == "RECORD") ? "R\n" : ((functionRunning == "RECORD GO") ? "P\n" : "N\n")); // [RECSTATUS]
}

//  _____________________________________
// /                                     \
//(  Autonomous Function Starts and Stop  )
// \_____________________________________/

void autonomousStart_GotoStart() {

}

void autonomousStart_GotoRange() {
  
}

void autonomousStart_Record() {
  digitalWrite(pinIndicator, HIGH);
}

void autonomousStart_RecordGo() {
  
}

void autonomousStart_Follow() {
  
}

void autonomousStop() {
  digitalWrite(pinIndicator, LOW);
}

//  ___________________________
// /                           \
//(  Autonomous Function Loops  )
// \___________________________/

void autonomousLoop_GotoStart() {
  
}

void autonomousLoop_GotoRange() {
  
}

void autonomousLoop_Record() {
  // Append current position to recording, rounded to 2 decimal places
}

void autonomousLoop_RecordGo() {
  // Is the bot at recording[playbackIndex]?
  // if not, go towards it
  // if so, increment playbackIndex and go towards the new recording[playbackIndex]
  // (unless end of recording, in which case stop function and reset playbackIndex to 0
}

void autonomousLoop_Follow() {
  
}

//  __________________
// /                  \
//(  Helper Functions  )
// \__________________/

