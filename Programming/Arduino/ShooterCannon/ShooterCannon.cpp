/*
  ShooterCannon.h - Library controlling a Pneumatic Cannon.
  Created by GISS, 2016.
*/

#include "Arduino.h"
#include "ShooterCannon.h"

// --<List of Variables>--
// int GPIO0;
// int PWM0;
// int PWM1;

// Remember to update ShooterCannon.h if these are changed!

ShooterCannon::ShooterCannon(int GPIO0, int PWM0, int PWM1) {
  _GPIO0 = GPIO0;
  _PWM0 = PWM0;
  _PWM1 = PWM1;
}

void ShooterCannon::setup() {
  // Place any setup code here
}

void ShooterCannon::reset() {
  // Place any cleanup code here, i.e. reset variables
}

void ShooterCannon::prepare() {
  // Place any code here in preparation for firing
  // i.e. reloading
}

void ShooterCannon::fire() {
  // Place firing code here
}