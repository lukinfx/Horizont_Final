

Přeskočit na obsah
Jak používat Gmail se čtečkou obrazovky
Meet
Nová schůzka
Připojit se
Hangouts

Konverzace

Nepřečtené
 
1–40 z 40
 

Vše ostatní
 
1–25 z 4 729
 
Lukas Frankl
OSM - PBF příkazy
 
Příloha:
convert.txt
Příloha:
convertAll.txt
17:04
Lukas Frankl
GPX files
 - Austria, Belgium, Czech Republic, France, Germany, Poland
Příloha:
GPXFiles.zip
13. 11.
"Hyundai Motor Czec.
Vybočte z řady s novým Hyundai Tucson
 
12. 11.
OSM - PBF příkazy
Doručená pošta

Lukas Frankl
Přílohy
17:04 (před 38 minutami)
komu: mně


2 přílohy

@echo ========================================================================================
@echo ========================================================================================
@echo Country: %1
@echo ========================================================================================
@echo ========================================================================================

@echo ----------------------------------------------------------------------------------------
@echo Deleting GPX file
@echo ----------------------------------------------------------------------------------------
del %1-peaks.gpx
del %1-viewtowers.gpx
del %1-transmitters.gpx

@echo ----------------------------------------------------------------------------------------
@echo Converting PBF to OSM
@echo ----------------------------------------------------------------------------------------
osmconvert\osmconvert.exe %1-latest.osm.pbf --drop-ways --drop-broken-refs --out-osm -o=temp.osm

@echo ----------------------------------------------------------------------------------------
@echo Extracting peaks
@echo ----------------------------------------------------------------------------------------
call osmosis-0.48.2\bin\osmosis.bat --read-xml temp.osm --tf accept-nodes --node-key-value keyValueList="natural.peak"  --write-xml temp-peaks.osm
OSMToGPX\OSMToGPX.exe temp-peaks.osm %1-peaks.gpx peak --exclude-no-name --exclude-no-elevation

@echo ----------------------------------------------------------------------------------------
@echo Extracting viewtowers
@echo ----------------------------------------------------------------------------------------
call osmosis-0.48.2\bin\osmosis.bat --read-xml temp.osm --tf accept-nodes --node-key-value keyValueList="tower:type.observation"  --write-xml temp-viewtowers.osm
OSMToGPX\OSMToGPX.exe temp-viewtowers.osm %1-viewtowers.gpx viewtower

@echo ----------------------------------------------------------------------------------------
@echo Extracting transmitters
@echo ----------------------------------------------------------------------------------------
call osmosis-0.48.2\bin\osmosis.bat --read-xml temp.osm --tf accept-nodes --node-key-value keyValueList="tower:type.communication"  --write-xml temp-transmitters.osm
OSMToGPX\OSMToGPX.exe temp-transmitters.osm %1-transmitters.gpx transmitter

@echo ----------------------------------------------------------------------------------------
@echo Extracting churches
@echo ----------------------------------------------------------------------------------------
call osmosis-0.48.2\bin\osmosis.bat --read-xml temp.osm --tf accept-nodes --node-key-value keyValueList="building.chapel" keyValueList="building.church"  --write-xml temp-churches.osm
OSMToGPX\OSMToGPX.exe temp-churches.osm %1-churches.gpx church
convert.txt
Zobrazování položky convert.txt.