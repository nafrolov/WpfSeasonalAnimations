# WpfSeasonalAnimations
Add seasonal animations in your apps with customizable wpf controls.
Based on [WpfSnowfall](https://github.com/Marplex/WpfSnowfall)

# Features

- Snowfall with different snowflakes
- Leaf fall with different tree leaves and wind gusts, leaf ground accumulation
- Flower bloom with flowers growing from the bottom and side edges
- Fully customizable

# Available for

- ![NET6](https://img.shields.io/badge/.NET-6.0-red)
- ![NETFramework48](https://img.shields.io/badge/.NET%20Framework-4.8-orange)
- ![NETFramework47](https://img.shields.io/badge/.NET%20Framework-4.7-orange)

# How to use

## Snowfall

Include WpfSnowfall user control in your layout. For more info view the `Demo` project.

```xml
<sf:Snowfall
    EmissionRate="5"
    Fill="White"
    ScaleFactor="1.1"
    OpacityFactor="1"
    LeaveAnimation="Fade"
    ParticleSpeed="1" />
```

#### Customize

| Property      	| Description                                                        	| Default 	|
|---------------	|--------------------------------------------------------------------	|---------	|
| EmissionRate  	| How many snowflakes to emit every second                           	| 5       	|
| ScaleFactor   	| Scale in/out snowflakes. Higher values generates bigger snowflakes 	| 1.0     	|
| OpacityFactor 	| Snowflake opacity                                                  	| 1.0     	|
| ParticleSpeed 	| Snowflake fall speed                                               	| 1.0     	|
| Fill          	| Snowflake color                                                    	| White   	|
| LeaveAnimation    | Snowflake leave animation                                          	| Fade    	|

## Leaf fall

Include LeafFallControl in your layout. For more info view the `Demo` project.

```xml
<lf:LeafFallControl
    EmissionRate="2"
    ScaleFactor="1.6"
    OpacityFactor="0.9"
    ParticleSpeed="0.5"
    LeaveAnimation="Fade"
    Wind="0.2"
    Gustiness="0.3"
    AccumulateOnGround="True"
    GroundLevel="0.9"
    MaxGroundLeaves="150"
    GroundFadeSeconds="0"/>
```

#### Customize

| Property      	 | Description                                                        	| Default 	| Recommended Range of values  |
|---------------	 |--------------------------------------------------------------------	|---------	| ---------------------------  |
| EmissionRate  	 | How many leaves to emit every second                             	| 5      	| 2...15                       |
| ScaleFactor   	 | Scale in/out leaves. Higher values generates bigger leaves    	    | 1.0     	| 0.6...2.0                    |
| OpacityFactor 	 | Leaf opacity                                                     	| 1.0     	| 0.5...1.0                    |
| ParticleSpeed 	 | Leaf fall speed                                                  	| 1.0     	| 0.5...2.0                    |
| Fill          	 | Leaf color. If null, each leaf uses random color from autumn palette | null      | -                            |
| LeaveAnimation     | Leaf leave animation                                          	    | Fade    	| -                            |
| Wind               | Horizontal wind strength in px/frame                            	    | 0.0    	| 0.0...3.0                    |
| Gustiness          | How much the wind fluctuates over time (0 = still, 1 = very gusty)   | 0.4   	| 0...1                        |
| AccumulateOnGround | Whether leaves should pile up at the bottom instead of vanishing     | True    	| -                            |
| GroundLevel        | Where the leaves accumulate relative to control(0 = top, 1 = bottom) | 1.0    	| 0.85...0.95                  |
| MaxGroundLeaves    | Maximum number of leaves that can rest on the ground            	    | 0     	| 50...150                     |
| GroundFadeSeconds  | If > 0, ground leaves fade out over this many seconds after landing  | 0     	| 0...6                        |

## Flower bloom

Include FlowerBloomControl in your layout. For more info view the `Demo` project.

```xml
<fb:FlowerBloomControl
    EmissionRate="4"
    ScaleFactor="1.6"
    OpacityFactor="0.95"
    ParticleSpeed="1.0"
    StemFill="#4CAF50"
    LeaveAnimation="Fade"
    FadeAfterBloomSeconds="8"
    BottomGroundLevel="0.15"
    SideGroundLevel="0.32"
    FlowerMargin="20"
    SpawnFromBottom="True"
    SpawnFromSides="True" />
```

#### Customize

| Property              | Description                                                                                     | Default 	| Recommended Range of values  |
|---------------       |----------------------------------------------------------------------------------------------- 	|---------	| ---------------------------  |
| EmissionRate          | How many flowers to emit every second                                                           | 3.0     	| 1...10                       |
| ScaleFactor           | Overall flower size multiplier                                                                  | 1.0     	| 0.5...2.0                    |
| OpacityFactor         | Opacity multiplier applied to each flower                                                       | 1.0     	| 0.5...1.0                    |
| ParticleSpeed         | Controls how quickly each flower grows and blooms                                               | 1.0     	| 0.5...2.0                    |
| Fill                  | Color of the opened flower cup. If null, each flower uses random color from built-in palette    | null      | -                            |
| StemFill              | Color of the stem, side leaf, and unopened bud                                                  | #4CAF50   | -                            |
| FlowerMargin          | Minimum horizontal distance, in pixels, between adjacent flowers                                | 20      	| 10...50                      |
| LeaveAnimation        | Flower leave animation after blooming                                                           | None    	| -                            |
| FadeAfterBloomSeconds | If LeaveAnimation is Fade, seconds to wait before fading begins                                 | 6.0     	| 0...30                       |
| BottomGroundLevel     | Fraction of the control height used by flowers blooming from the bottom edge                    | 0.15    	| 0.05...0.3                   |
| SideGroundLevel       | Fraction of the control width used by flowers blooming from the left and right edges            | 0.15    	| 0.1...0.35                   |
| SpawnFromBottom       | Whether flowers bloom from the bottom edge                                                      | True    	| -                            |
| SpawnFromSides        | Whether flowers bloom from the left and right edges                                             | True    	| -                            |

# 📜 License

```xml
Copyright (c) 2026 nafrolov

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```