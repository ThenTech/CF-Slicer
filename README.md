# Computational Fabrication 3D model Slicer

> Group 3 - **Lieven Libberecht, William Thenaers**

## Minimal requirements

- [x] Program with input STL/OBJ file and output [gcode](https://reprap.org/wiki/G-code) file
- [x] Basic GUI to go through slices step by step 
- [x] (+ render all paths in colors)
- [x] Basic GUI for configuring essential parameters e.g. layer height, nozzle diameter, number of shells,…
- [x] Support for Shells + infill (basic e.g. rectangular) 
- [x] (+ roofs/floors)
- [x] Models can have holes!
- [x] Support generation (basic rectangle structure)
- [x] Optimize for print quality (not speed)

## Possible extras
- [x] Other types of infill (+ size)
- [ ] Adaptive slicing (thinner slices for some parts)
- [ ] Support for bridges (not requiring support) => nozzle cannot start in a void
- [ ] ~? Optimizing paths for speed
- [x] Support structures to avoid toppling of objects (e.g. cube standing on 1 corner)
- [ ] Automatic orientation of model (XY rotation; optimization)
- [x] Manual orientation of model (XYZ rotation and scale)
- [x] Automatic centring of model (XY position)
- [x] Indication of bounding box size and warning if it's larger than the Ender3 build plate (text turns red)
- [x] Optimized support structures (zigzag + others + size)
- [x] Adhesion with brim/skirt (+ size)
- [ ] Print time estimation (take speed per accumulated extrusion and sum them)
- [x] Adjust slicing settings at runtime
  - Nozzle thickness (layer height)
  - Nozzle diameter (line thickness)
  - Filament diameter (optimized value for extrusion)
  - Number of shells (surfaces and borders)
- [x] Preview of 3D object and drawing of colour coded polygons (with adjustable thickness and direction arrows)
  - <span style="background-color:#000;">     </span> Contour polygons
  - <span style="background-color:#BBB;">     </span> Contour shell polygons
  - <span style="background-color:#00F;">     </span> Hole polygons
  - <span style="background-color:#0AE;">     </span> Hole shell polygons
  - <span style="background-color:#8B0000;">     </span> Surface fill lines
  - <span style="background-color:#F00;">     </span> Infill lines
  - <span style="background-color:#9ACD32;">     </span> Adhesion and support lines
  - <span style="background-color:#FFC0CB;">     </span> Polygons that appeared "open" but were "fixed" to include them anyway
- [x] Progress indication during slicing (also indicated by same colours for every phase)
- [x] Slicing is multithreaded where possible
- [x] Arc detection for polygons to see if it's a circle, so we could print an arc in GCode instead, but the firmware does not have this enabled, so it is commented out.

## Progress

- [x] Algorithm + UI for basic slicing of 3D model (3D view for STL visualizer + 2D view for 
  showing slices)
- [x] Erode perimeter with half the nozzle thickness (otherwise print will be too large)
- [x] **[Deadline 14/11]** Generate g-code for a perimeter of a single slice 
- [x] And try 3D printing
- [x] Extend data structure to support holes in object/polygon
- [x] Generate second shell
- [x] Generate basic rectangular infill structure (line per line intersection between grid and
  polygon slices)
- [x] **[Deadline 7/12]** Extend g-code generation and 3D print simple object that does not require support 
  structure
- [x] Calculate regions + generate paths (+ g-code) for floors and roofs
- [x] Try 3D printing a closed object (roofs + floors) that does not require support
- [x] Features + algorithms for support structure generation
- [x] **[Deadline 6/01]** Implement all other minimal requirements (e.g. basic UI controls for settings)

## Problems

- On rare occasions, some points or triangles disappear, resulting in layers or partial layers to be empty, probably in the algorithm to connect polygons
  - They go away resulting in other polies to be wrongly marked as contour or hole
- Zigzag support sometimes disappears on some layers
- Stringing issues, despite doing the same things as Cura. Some strings cause the connections to be solid.
- Overlapping shells are not joined together, but are removed.
- Sometimes "phantom surfaces" are found and filled in, although visually there shouldn't be one

## TODO

- [x] Scale slider naar 1 dim ==> 1.0 == 1 mm
- [x] ~~Sliders met textfield voor manual input~~ (of sliders weg en enkel [real scale; percentage])
- [x] Clipper (polygon intersection etc)?
- [x] 2de viewport voor 1 slice te previewen => met slider voor slice te selecteren
- [x] Slice preview met kleuraanduiding in object of bovenkant object wegnemen om slice te zien
- [x] Nozzle thickness parameter
- [x] Indicatie van grootte
- [x] XY translate en rotate op object voor te slicen (=> transform individual polygons...)
- [x] Slice opstellen door uinion van vlak met dikte NozzleThickness en object (met Clipper?)
- [x] Slice opstellen door triangle en plane intersection ipv 3D intersection
- [x] Add ambient light ~~
- [x] Basic SliceModel class with slice plane and sliced model preview
- [x] Add brim/skirt (erode to outer on slice 0)
- [x] Make offsets for every contour/hole and then union them
- [x] Surface (floor/roof) herkenning
  - ✔ Refactor code to split generation
  - ✔ Subtract Layer+1 from Layer+0 for roofs
  - ✔ Subtract Layer+0 from Layer+1 for floors
  - ✔ Add to separate list
- [x] Surface invullen met patroon => denser infill, ~~mss met offsets ipv rects or zigzag?~~
  - ✔ Shells en dense infill beter 1.5*nozzle thinkness => Cura
  - ✔ Dense infill korte segmentjes verwijderen na clipping
    - ✔ OF Line clipping in de plaats => Paths met lines clippen met inner shell
- [x] Overhang herkenning voor support (diff van subtraction > nozzle width => support needed)
- [x] Support toevoegen voor overgang
- [x] Check Cura output to identify stringing issue? Nothing found...
    -  ==> The solution is probably to retract and reset extrusion if the travel distance to the next polygon is too big (>5 mm or something)
- [x] Report schrijven