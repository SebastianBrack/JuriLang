# juri lang
[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://github.com/SebastianBrack/JuriLang) 
# Liste aller Keywords

>Online Interpreter mit Texteditor und Syntax Highlighting auf Heroku
-> [Click to tryout Juri online :)](https://tryjuribeta.herokuapp.com/)


* if
```python
if a < b then return a
```
* return
* repeat
* init
* fun
```python
fun add l r
	return l+r
# funktionsdefinition mit name add und 2 parametern l und r

```
* operator
```python
operator *- l r
	return (l*r)-69
# definition eines neuen operators *- 

```
* as
* iterate
* break
* import
```python
import test/testme.jri

-> derzeit nur ein import pro Datei, importiert parst und interpretiert den Inhalt der anderen Datei
* siehe importfun
```
### standard Bibliothek
* rand()
> rand(start=optional(float),end=float)
> der end parameter ist exklusiv
wenn rand() nur einen Parameter erhält wird der start der random range automatisch auf 0 gesetzt
```python
n = rand(1 10)
# n evaluiert zu einer zufälligen Zahl zwischen 1 und 9
i = rand(10)
# i evaluiert zu einer zufälligen Zahl zwischen 0 und 9
```
* print()
	nimmt eine liste von floats und printet sie ohne newline am Ende
* printn()
	nimmt eine liste von floats und printet sie mit newline am Ende
* printc()
	nimmt eine liste von floats und wandelt den wert über die Ascii Tabelle in einen char um mit newline am Ende
	```python
	printc(72 101 108 108 111 32 87 111 114 108 100 33 )
	# printet Hello World! in die Konsole
	```
### Operator-Zeichen:
```python
+-*/><.=!%
```

### Trennzeichen:
```python
()[]
```

### Kommentare:
Kommentar derzeit nur am Anfang der Zeile
```python
i = 0
print(i + 420)
# Ich bin ein Kommentar
print(69)
```


# Arrays
Arrays haben eine **feste** Größe
**Arraynamen** beginnen mit einem ```:```.
Arrays werden mit folgender Sytax deklariert:
```python
:myList = [1 2 3 4]          
# Erstellt die Liste mit den gegebenen Elementen

:anotherList = [2 to 345]    
# Erstellt eine Liste mit den Zahlen von 2 bis 345

:longList = init 1000 0      
# Erstellt eine Liste mit 1000 nullen

:evenNums = init 10 as i
    i * 2                    
# Erstellt eine Liste mit den Zahlen [0 2 4 8 10 12 14 16 18] 
```

Einzelne Arrayelemente können per **Index** referenziert und geändert werden.  Das Erste Element hat den Index 0.
```python
print(0:myList)     
# gibt 1 aus
print(-1:myList)    
# gibt 4 aus
2:myList = 99 
# weist dem Element an Index 2 den wert 99 zu: [1 2 99 4]
```

Um die **Länge** eines Arrays herrauszufinden fragen Es einfach.
```python
?:myList            
# evaluiert zu 4
```

Um über ein Array zu **iterieren** stellt juri die ```iterate``` Anweisung zur Verfügung.
```python
iterate :myList as x
    print(x)
```

in einer if schleife können wir mit dem keyword ```break``` den schleifendurchlauf unterbrechen
```python
i = 0
if i < 10 repeat
	if i == 5
		printn(i)
		break
	i=i+1
# printet 5 in die konsole
```


Hier noch ein Beispiel wie man mit einer Klassischen If-Schleife über ein Array iteriert.
```python
i = 0
if i < ?:myList repeat
    printn(i:myList)
    i = i+1
```

**Array Pointer**
```python
# Dieses Beispiel demonstriert den Einsatz von Array-Pointern

:nums = [1 to 10]

fun map :list
#alle elemente der Ursprungsliste werden mit 2 multipliziert
        i=0
        if i < ?:list repeat
                i:list = i:list * 2
                i=i+1
        0	
# 0 am Ende weil Funktionen immer etwas returnen müssen was ist jedoch egal
                
map(:nums)

iterate :nums as x
        print(x)

# output: 2 4 6 8 10 12 14 16 18 20
```
**Inline Arraydefinierung**

```python
fun sniplist :arr low high
    iterate [low to high] as i
        print(i:arr)
    printcn()    
        
:nums = [0 to 100]      

sniplist(:nums 2 5)
sniplist([4 8 564 99  12154 54] 2 5)
sniplist([222 to 333] 2 5)

```


>*schaut gerne im JuriConsole/examples Ordner vorbei, dort findet ihr ein paar Programme die Juri geschrieben sind :)*
___

	
