/*
  ShooterCannon.h - Library controlling a Pneumatic Cannon.
  Created by GISS, 2016.
*/
#ifndef ShooterCannon_h
#define ShooterCannon_h

#include "Arduino.h"

class ShooterCannon {
  public:
    ShooterCannon(int GPIO0, int PWM0, int PWM1);
	void setup();
	void reset();
	void prepare();
	void fire();
  private:
    int _GPIO0;
	int _PWM0;
	int _PWM1;
};

#endif