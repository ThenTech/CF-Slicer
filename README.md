# Computational Fabrication 3D model Slicer

> **Lieven Libberecht, William Thenaers**

## Minimal requirements

- [ ] Program with input STL/OBJ file and output gcode file
- [ ] Basic GUI to go through slices step by step (+ render all paths in colors)
- [ ] Basic GUI for configuring essential parameters e.g. layer height, nozzle diameter, number of shells,…
- [ ] Support for Shells + infill (basic e.g. rectangular) + roofs/floors
- [ ] Models can have holes!
- [ ] Support generation (basic zig-zag structure)
- [ ] Optimize for print quality (not speed)

## Possible extras
- [ ] Other types of infill
- [ ] Adaptive slicing
- [ ] Support for bridges (not requiring support) => nozzle cannot start in a void
- [ ] Optimizing paths for speed
- [ ] Support structures to avoid toppling of objects (e.g. cube standing on 1 corner)
- [ ] Automatic orientation of model (optimization)
- [ ] Optimized support structures

## Progress

- [ ] Algorithm + UI for basic slicing of 3D model (3D view for STL visualizer + 2D view for 
  showing slices)
  - [x] Basic 3D UI (William), uses:
    - [helix-toolkit](https://github.com/helix-toolkit/helix-toolkit) in a C# WPF 3D form
    - [clipper](http://www.angusj.com/delphi/clipper.php)
    - [AssimpNet](https://bitbucket.org/Starnick/assimpnet)
  - [x] Basic GCode writer (uses [GCodeNET](https://github.com/chrismiller7/GCodeNet)) (William)
- [ ] Erode perimeter with half the nozzle thickness (otherwise print will be too large) 
  clipper
- [ ] Generate g-code for a perimeter of a single slice and try 3D printing
- [ ] Extend data structure to support holes in object/polygon
- [ ] Generate second shell
- [ ] Generate basic rectangular infill structure (line per line intersection between grid and
  polygon slices)
- [ ] Extend g-code generation and 3D print simple object that does not require support 
  structure
- [ ] Calculate regions + generate paths (+ g-code) for floors and roofs
- [ ] Try 3D printing a closed object (roofs + floors) that does not require support
- [ ] Features + algorithms for support structure generation
- [ ] Implement all other minimal requirements (e.g. basic UI controls for settings)