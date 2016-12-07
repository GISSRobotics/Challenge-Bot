/*
  ShooterPitcher.h - Library controlling a Pitching Shooter.
  Created by GISS, 2016.
*/
#ifndef ShooterPitcher_h
#define ShooterPitcher_h

#include "Arduino.h"

class ShooterPitcher {
  public:
    ShooterPitcher(int GPIO0, int PWM0, int PWM1);
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