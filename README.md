<h1>RobotArmFrontEnd</h1>
Building a new and better front end for the Robot Arms.

<h2>As-is Conditions</h2>
SupportLib includes references to missing C++ headers:</br>
(kollmorgen_s200: robot arm servo drives)
<ul><li>stdmpi.h</li>
<li>firmware.h</li>
<li>kollmorgen_s200.h</li></ul>

Robot Dancing:</br>
There exists code for:
<ul><li>robot movement controlled via axis angles</li>
<li>robot movements in XYZ format from dxf file</li></ul>

Robot Arm Geometry has following problems
<ul><li>pathing and homing doesn't include axial rotation</li>
<li>tool positioning is not included in calculations</li>
<li>collision detection not included in software</li>
<li>hardware dependencies are limiting</li></ul>

<h2>Development Plan</h2>
File Interpreter</br>
Blender Simulator</br>
Python Interface (Post-processor)

<h2>License</h2>
<p>Licensed under Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0);
