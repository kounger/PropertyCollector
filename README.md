# PropertyCollector

A little program I wrote based on all the methods I created by working on a plugin.

This program uses reflection to extract all Properties from a type and all its nested types if wanted and saves them inside a dictionary. 
These properties can then be called via their canonical name. If these properties inside the dictionary are connected with an object the value 
can be extracted via the canonical name of the Property as well. If the .cs-File of the type is available it can be used to extract the xml-summary
of a property via the use of Roslyn. The PropertyProcessor class includes methods to print all Properties and a method to write all properties to a csv file.
