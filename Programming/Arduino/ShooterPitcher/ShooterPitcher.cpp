/*
  ShooterPitcher.h - Library controlling a Pitching Shooter.
  Created by GISS, 2016.
*/

#include "Arduino.h"
#include "ShooterPitcher.h"

// --<List of Variables>--
// int GPIO0;
// int PWM0;
// int PWM1;

// Remember to update ShooterPitcher.h if these are changed!

ShooterPitcher::ShooterPitcher(int GPIO0, int PWM0, int PWM1) {
  _GPIO0 = GPIO0;
  _PWM0 = PWM0;
  _PWM1 = PWM1;
}

void ShooterPitcher::setup() {
  // Place any setup code here
}

void ShooterPitcher::reset() {
  // Place any cleanup code here, i.e. reset variables
}

void ShooterPitcher::prepare() {
  // Place any code here in preparation for firing
  // i.e. reloading
}

void ShooterPitcher::fire() {
  // Place firing code here
}