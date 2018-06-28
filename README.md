# PropertyCollector

A little program I wrote based on all the methods I created by working on a plugin.

This program uses reflection to extract all Properties from a type or an object. It can also extract properties of nested classes. These properties will be saved in a dictionary from where they can be called via their canonical name. If object properties are collected, the canonical name can also be used to retrieve property values. If the .cs-File of the type is available it can be used to extract the xml-summary of a property via the use of Roslyn as well. The PropertyProcessor class includes methods to print all Properties and a method to write all properties to a csv file. 

All in all this program can be useful when working with classes with a large number of properties.
