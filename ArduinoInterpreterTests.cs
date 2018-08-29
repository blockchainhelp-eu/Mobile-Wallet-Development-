﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using CLanguage.Syntax;
using CLanguage.Types;
using static CLanguage.CLanguageService;
using CLanguage.Interpreter;

namespace CLanguage.Tests
{
    [TestClass]
    public class ArduinoInterpreterTests
    {
        ArduinoTestMachineInfo.TestArduino Run (string code)
        {
            var machine = new ArduinoTestMachineInfo ();
            var fullCode = code + "\n\nvoid main() { __cinit(); setup(); while(1){loop();}}";
            var i = CLanguageService.CreateInterpreter (fullCode, machine, new TestPrinter ());
            i.Reset ("main");
            i.Step ();
            return machine.Arduino;
        }

        [TestMethod]
        public void Sizes ()
        {
            var ec = new EmitContext (new ArduinoTestMachineInfo (), new Report (new TestPrinter ()));

            Interpreter.CompiledVariable ParseVariable (string code)
            {
                var exe = Compile (code, MachineInfo.Windows32, new TestPrinter ());
                return exe.Globals.Skip (1).First ();
            }

            var charV = ParseVariable ("char v;");
            Assert.AreEqual (charV.VariableType.GetByteSize (ec), 1);

            var intV = ParseVariable ("int v;");
            Assert.AreEqual (intV.VariableType.GetByteSize (ec), 2);

            var shortIntV = ParseVariable ("short int v;");
            Assert.AreEqual (shortIntV.VariableType.GetByteSize (ec), 2);

            var unsignedLongV = ParseVariable ("unsigned long v;");
            Assert.AreEqual (unsignedLongV.VariableType.GetByteSize (ec), 4);

            var unsignedLongLongV = ParseVariable ("unsigned long long v;");
            Assert.AreEqual (unsignedLongLongV.VariableType.GetByteSize (ec), 8);

            var intPV = ParseVariable ("int *v;");
            Assert.AreEqual (intPV.VariableType.GetByteSize (ec), 2);

            var longPV = ParseVariable ("long *v;");
            Assert.AreEqual (longPV.VariableType.GetByteSize (ec), 2);
        }

        public const string BlinkCode = @"
void setup() {                
  // initialize the digital pin as an output.
  // Pin 13 has an LED connected on most Arduino boards:
  pinMode(13, OUTPUT);     
}

void loop() {
  digitalWrite(13, HIGH);   // set the LED on
  delay(1000);              // wait for a second
  digitalWrite(13, LOW);    // set the LED off
  delay(1000);              // wait for a second
}
";

        [TestMethod]
        public void Blink ()
        {
            var arduino = Run (BlinkCode);

            Assert.AreEqual (1, arduino.Pins[13].Mode);
        }

        [TestMethod]
        public void AnalogReadSerial ()
        {
            var code = @"
void setup() {
  Serial.begin(9600);
}

void loop() {
  int sensorValue = analogRead(A0);
  Serial.println(sensorValue);
  delay(1);
}";
            var arduino = Run (code);
            Assert.AreEqual ("42", arduino.SerialOut.ToString ().Split ("\n").First ().Trim ());
        }

        [TestMethod]
        public void DigitalReadSerial ()
        {
            var code = @"
void setup() {
  Serial.begin(9600);
  pinMode(2, INPUT);
}

void loop() {
  int sensorValue = digitalRead(2);
  Serial.println(sensorValue, DEC);
}
";
            var arduino = Run (code);
            Assert.AreEqual ("0", arduino.SerialOut.ToString ().Split ("\n").First ().Trim ());
        }

        [TestMethod]
        public void DigitalRead ()
        {
            var code = @"
void setup() {
  pinMode(2, INPUT);
  pinMode(3, OUTPUT);
}

void loop() {
  int sensorValue = digitalRead(2);
  digitalWrite(3, sensorValue);
}
";
            var arduino = Run (code);
            Assert.AreEqual (0, arduino.Pins[2].Mode);
            Assert.AreEqual (1, arduino.Pins[3].Mode);
        }

        public const string FadeCode = @"
int brightness = 0;    // how bright the LED is
int fadeAmount = 5;    // how many points to fade the LED by

void setup()  { 
  // declare pin 9 to be an output:
  pinMode(9, OUTPUT);
} 

void loop()  { 
  // set the brightness of pin 9:
  analogWrite(9, brightness);    

  // change the brightness for next time through the loop:
  brightness = brightness + fadeAmount;

  // reverse the direction of the fading at the ends of the fade: 
  if (brightness == 0 || brightness == 255) {
    fadeAmount = -fadeAmount ; 
  }     
  // wait for 30 milliseconds to see the dimming effect    
  delay(30);                            
}
";

        [TestMethod]
        public void Fade ()
        {
            var arduino = Run (FadeCode);
        }

        [TestMethod]
        public void Tone ()
        {
            var code = @"
int melody[] = {
  NOTE_C4, NOTE_G3,NOTE_G3, NOTE_A3, NOTE_G3,0, NOTE_B3, NOTE_C4};

// note durations: 4 = quarter note, 8 = eighth note, etc.:
int noteDurations[] = {
  4, 8, 8, 4,4,4,4,4 };

void setup() {
  // iterate over the notes of the melody:
  for (int thisNote = 0; thisNote < 8; thisNote++) {

    // to calculate the note duration, take one second 
    // divided by the note type.
    //e.g. quarter note = 1000 / 4, eighth note = 1000/8, etc.
    int noteDuration = 1000/noteDurations[thisNote];
    tone(8, melody[thisNote],noteDuration);

    // to distinguish the notes, set a minimum time between them.
    // the note's duration + 30% seems to work well:
    int pauseBetweenNotes = noteDuration * 1.30;
    delay(pauseBetweenNotes);
    // stop the tone playing:
    noTone(8);
  }
}

void loop() {
  // no need to repeat the melody.
}
";
            var arduino = Run (code);
        }

        [TestMethod]
        public void Calibration ()
        {
            var code = @"
// These constants won't change:
const int sensorPin = A0;    // pin that the sensor is attached to
const int ledPin = 9;        // pin that the LED is attached to

// variables:
int sensorValue = 0;         // the sensor value
int sensorMin = 1023;        // minimum sensor value
int sensorMax = 0;           // maximum sensor value


void setup() {
  // turn on LED to signal the start of the calibration period:
  pinMode(13, OUTPUT);
  digitalWrite(13, HIGH);

  // calibrate during the first five seconds 
  while (millis() < 1) {
    sensorValue = analogRead(sensorPin);

    // record the maximum sensor value
    if (sensorValue > sensorMax) {
      sensorMax = sensorValue;
    }

    // record the minimum sensor value
    if (sensorValue < sensorMin) {
      sensorMin = sensorValue;
    }
  }

  // signal the end of the calibration period
  digitalWrite(13, LOW);
}

void loop() {
  // read the sensor:
  sensorValue = analogRead(sensorPin);

  // apply the calibration to the sensor reading
  sensorValue = map(sensorValue, sensorMin, sensorMax, 0, 255);

  // in case the sensor value is outside the range seen during calibration
  sensorValue = constrain(sensorValue, 0, 255);

  // fade the LED using the calibrated value:
  analogWrite(ledPin, sensorValue);
}
";
            var arduino = Run (code);
        }

        [TestMethod]
        public void StateChangeDetection ()
        {
            var code = @"

// this constant won't change:
const int  buttonPin = 2;    // the pin that the pushbutton is attached to
const int ledPin = 13;       // the pin that the LED is attached to

// Variables will change:
int buttonPushCounter = 0;   // counter for the number of button presses
int buttonState = 0;         // current state of the button
int lastButtonState = 0;     // previous state of the button

void setup() {
  // initialize the button pin as a input:
  pinMode(buttonPin, INPUT);
  // initialize the LED as an output:
  pinMode(ledPin, OUTPUT);
  // initialize serial communication:
  Serial.begin(9600);
  Serial.println(""start"");
}


void loop() {
  // read the pushbutton input pin:
  buttonState = digitalRead(buttonPin);

  // compare the buttonState to its previous state
   if (buttonState != lastButtonState) {
    // if the state has changed, increment the counter
     if (buttonState == HIGH) {
      // if the current state is HIGH then the button went from off to on:
       buttonPushCounter++;
       Serial.println(""on"");
       Serial.print (""number of button pushes: "");
            Serial.println (buttonPushCounter);
        } else {
      // if the current state is LOW then the button went from on to off:
       Serial.println(""off"");
    }
    // Delay a little bit to avoid bouncing
     delay (50);
    }
// save the current state as the last state, for next time through the loop
lastButtonState = buttonState;


  // turns on the LED every four button pushes by checking the modulo of the
  // button push counter. the modulo function gives you the remainder of the
  // division of two numbers:
   if (buttonPushCounter % 4 == 0) {
     digitalWrite (ledPin, HIGH);
   } else {
     digitalWrite (ledPin, LOW);
   }

}";
            var arduino = Run (code);
            Assert.AreEqual ("start", arduino.SerialOut.ToString ().Split ("\n").First ().Trim ());
        }

    }
}
