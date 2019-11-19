# Computational Fabrication 3D model Slicer

> **Lieven Libberecht, William Thenaers**

## Minimal requirements

- [x] Program with input STL/OBJ file and output [gcode](https://reprap.org/wiki/G-code) file
- [x] Basic GUI to go through slices step by step 
- [x] (+ render all paths in colors)
- [x] Basic GUI for configuring essential parameters e.g. layer height, nozzle diameter, number of shells,…
- [ ] Support for Shells + infill (basic e.g. rectangular) + roofs/floors
- [x] Models can have holes!
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

- [x] Algorithm + UI for basic slicing of 3D model (3D view for STL visualizer + 2D view for 
  showing slices)
- [x] Erode perimeter with half the nozzle thickness (otherwise print will be too large)
- [x] **[Deadline 14/11]** Generate g-code for a perimeter of a single slice 
- [ ] And try 3D printing
- [x] Extend data structure to support holes in object/polygon
- [ ] Generate second shell
- [ ] Generate basic rectangular infill structure (line per line intersection between grid and
  polygon slices)
- [ ] **[Deadline 7/12]** Extend g-code generation and 3D print simple object that does not require support 
  structure
- [ ] Calculate regions + generate paths (+ g-code) for floors and roofs
- [ ] Try 3D printing a closed object (roofs + floors) that does not require support
- [ ] Features + algorithms for support structure generation
- [ ] Implement all other minimal requirements (e.g. basic UI controls for settings)

## TODO

- [x] Scale slider naar 1 dim ==> 1.0 == 1 mm
- [ ] ~~Sliders met textfield voor manual input~~ (of sliders weg en enkel [real scale; percentage])
- [ ] Clipper (polygon intersection etc)?
- [x] 2de viewport voor 1 slice te previewen => met slider voor slice te selecteren
- [x] Slice preview met kleuraanduiding in object of bovenkant object wegnemen om slice te zien
- [x] Nozzle thickness parameter
- [x] Indicatie van grootte
- [x] XY translate en rotate op object voor te slicen (=> transform individual polygons...)
- [x] Slice opstellen door uinion van vlak met dikte NozzleThickness en object (met Clipper?)
- [x] Slice opstellen door triangle en plane intersection ipv 3D intersection
- [x] Add ambient light ~~
- [x] Basic SliceModel class with slice plane and sliced model preview