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
double[] currentPos = { 0, 0 };                       // Current relative position of bot, roughly in meters
double[] currentVector = { 0, 0 };
double[] currentMotorSpeeds = { 0, 0 };
double[] requestedMotorSpeeds = { 0, 0 };
String functionRunning = "";
String commandString = "";
//ShooterPitcher shooterPitcher(iGPIO0, iPWM0, iPWM1);
//ShooterCannon shooterCannon(iGPIO0, iPWM0, iPWM1);

// --<Autonomous Function Specific Variables>--
double[][] recording = { {0, 0} };
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
  functionLoopBroker();
  sendStatus();
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
  // SETMOTORS X,Y[,T]                                    // Set motors to speeds [X, Y], optionally for T milliseconds
  // FIRE
  // SETSTART
  // GOTO START|RANGE
  // RECORD START|STOP|GO                         
  // STOP
  // FOLLOW
  if (commandstring == "STOP") {
    functionRunning = "";
    requestedMotorSpeeds = { 0, 0 };
  } else if (functionRunning == "") {
    if (commandString.startsWith("SETMOTOR") {
      // Count commas: if 1, parse for X and Y; if 2, also parse for T
    } else if (commandString == "FIRE" and ammoRemaining > 0) {
      if (isCannon) {
        //shooterCannon.prepare();
        //shooterCannon.fire();
      } else {
        //shooterPitcher.prepare();
        //shooterPitcher.fire();
      }
      ammoRemaining--;
    } else if (commandString == "SETSTART") {
      currentPos = { 0, 0 };
      currentVector = { 0, 0 };
    } else if (commandString.startsWith("GOTO")) {
      functionRunning = commandString;
    } else if (commandString.startsWith("RECORD")) {
      if (commandString.endsWith("START")) {
        functionRunning = "RECORD";
      } else if (commandString.endsWith("STOP")) {
        functionRunning = "";
      } else if (commandString.endsWith("GO")) {
        functionRunning = "RECORD GO";
      }
    } else if (commandString == "FOLLOW") {
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
  }
}

void sendStatus() {
  // Send a status string in the format:
  // [STATUS] [MOTORS] [VECTOR] [POSITION] [SEL] [IR] [AMMO] [RECSTATUS]
  Serial.print("OK 0,0 0,0 0,0 0 0000 5 N");
}

//  ___________________________
// /                           \
//(  Autonomous function loops  )
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

