# juri lang
[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://www.gitpod.io/#https://github.com/SebastianBrack/JuriLang) 
# List of all keywords

>Online interpreter with text editor and syntax highlighting on Heroku
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
# function definition with name add and 2 parameters l and r

```
* operator
```python
operator *- l r
	return (l*r)-69
# definition of a new operator *- 

```
* as
* iterate
* break
* import
```python
import test/testme.jri

-> currently only one import per file, imports parses and interprets the content of the other file
* see importfun
```
### standard library
* rand()
> rand(start=optional(float),end=float)
> the end parameter is exclusive
if rand() gets only one parameter the start of the random range is automatically set to 0
```python
n = rand(1 10)
# n evaluates to a random number between 1 and 9
i = rand(10)
# i evaluates to a random number between 0 and 9
```
* print()
	takes a list of floats and prints it without newline at the end
* printn()
	takes a list of floats and prints it with newline at the end
* printc()
	takes a list of floats and converts the value via the ascii table into a char with newline at the end
	```python
	printc(72 101 108 108 111 32 87 111 114 108 100 33 )
	# prints "Hello World!" to the console
	```
### Operator character:
```python
+-*/><.=!%
```

### Separators:
```python
()[]
```

### Comments:
Comments currently only at the beginning of the line
```python
i = 0
print(i + 420)
# I am a comment
print(69)
```


# Arrays
Arrays have a **fixed** size
**Array names** begin with a ```:```.
Arrays are declared with the following syntax:
```python
:myList = [1 2 3 4]          
# Creates a list with the given elements

:anotherList = [2 to 345]    
# Creates a list with the numbers from 2 to 345

:longList = init 1000 0      
# Creates a list with 1000 zeros

:evenNums = init 10 as i
    i * 2                    
# Creates a list with the numbers [0 2 4 8 10 12 14 16 18] 
```

Individual array elements can be referenced and changed by **Index**.  The first element has the index 0.
```python
print(0:myList)     
# gibt 1 aus
print(-1:myList)    
# gibt 4 aus
2:myList = 99 
#  assigns the value 99 to the element at index 2: [1 2 99 4]
```

To find out the **length** of an array just use the ? operator in front of the array name
```python
?:myList            
# evaluiert zu 4
```

To **iterate** over an array, juri provides the  ```iterate``` statement.
```python
iterate :myList as x
    print(x)
```

n an if loop we can use the keyword ```break``` to interrupt the loop.
```python
i = 0
if i < 10 repeat
	if i == 5
		printn(i)
		break
	i=i+1
# prints 5 in the console
```


Here is another example of how to iterate over an array using a classic if loop.
```python
i = 0
if i < ?:myList repeat
    printn(i:myList)
    i = i+1
```

**Array Pointer**
```python
# This example demonstrates the use of array pointers

:nums = [1 to 10]

fun map :list
#All elements of the origin list are multiplied by 2.
        i=0
        if i < ?:list repeat
                i:list = i:list * 2
                i=i+1
        0	
# 0 at the end because functions always have to return something, but what doesn't matter
                
map(:nums)

iterate :nums as x
        print(x)

# output: 2 4 6 8 10 12 14 16 18 20
```
**Inline array definition**

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


>*have a look at the JuriConsole/examples folder, there you will find some programs written in Juri :)*
___

	
