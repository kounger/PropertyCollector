using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace PropertyCollector
{
    public class Property
    {
        public string PropertyName { get; } = null;
        public string PropertyClassName { get; } = null;
        public string PropertyCanonicalName { get; } = null; //sequence to call property as string
        public PropertyInfo PropertyPropInfo { get; } = null;
        public string PropertyDescription { get; set; } = null;
        public Type PropertyType { get; } = null;
        public object PropertyObject { get; set; } = null;
        public dynamic PropertyValue
        {
            get
            {
                if (PropertyObject != null)
                {
                    return this.PropertyPropInfo.GetValue(this.PropertyObject);
                }
                else
                {
                    throw new System.NullReferenceException("PropertyObject is null.");
                }
               
            }
        }

        public Property(string propertyName, string propertyClassName, string propertyCanonicalName, PropertyInfo propertyPropInfo)
        {
            this.PropertyName = propertyName;
            this.PropertyClassName = propertyClassName;
            this.PropertyCanonicalName = propertyCanonicalName;
            this.PropertyPropInfo = propertyPropInfo;
            this.PropertyType = propertyPropInfo.ReflectedType;
        }
    }

    public class Properties
    {
        public Dictionary<string, Property> PropertyDictionary { get; } = new Dictionary<string, Property>();
        List<string> baseClasses = new List<string>();


        /// <summary>
        /// This method collects all Property(s) from a type and adds them to the PropertyDictionary of this class.
        /// If searchNestedClasses is true, all properties of the nested types of the given type are added as well.
        /// typeFilePath is a path to the .cs-File of a type and can be used to extract property xml-summaries via Roslyn.
        /// Warning: Properties with the same canonical name will be replaced inside the PropertyDictionary.
        /// </summary>
        public void addTypeProperties(Type type, bool searchNestedClasses, string typeFilePath = null)
        {
            addPropertiesUniversal(type, searchNestedClasses, typeFilePath: typeFilePath);
        }

        /// <summary>
        /// This method collects all Property(s) from an object and adds them to the PropertyDictionary of this class.
        /// typeFilePath is a path to the .cs-File of a type and can be used to extract property xml-summaries via Roslyn.
        /// All Property(s) will be linked with the object inside the PropertyDictionary of this class.
        /// Warning: Properties with the same canonical name will be replaced inside the PropertyDictionary. 
        /// Instead of adding another object of the same type to the PropertiesDictionary create a new Properties object!
        /// </summary>
        public void addObjectProperties(object obj, string typeFilePath = null)
        {
            addPropertiesUniversal(obj.GetType(), false, obj: obj, typeFilePath: typeFilePath);
        }

        /// <summary>
        /// This method collects all Property(s) from a type and adds them to the PropertyDictionary of this class.
        /// If searchNestedClasses is true, all properties of the nested types of the given type are added as well.
        /// typeFilePath is a path to the .cs-File of a type and can be used to extract property xml-summaries via Roslyn.
        /// Warning: Properties with the same canonical name will be replaced inside the PropertyDictionary.
        /// </summary>
        private void addPropertiesUniversal(Type type, bool searchNestedClasses, object obj = null,  string typeFilePath = null)
        {
            //If type is a nested type then collect all classes up to the base class:
            collectBaseClasses(type);            
            
            //Collect all properties of type:
            Dictionary<string, Property> typeProperties = new Dictionary<string, Property>();
            typeProperties = collectProperties(type, new List<string>(), searchNestedClasses, typeProperties);

            //Add object to all these properties of this type:
            foreach (Property prop in typeProperties.Values)
            {
                if (obj != null)
                {
                    if (obj.GetType() == prop.PropertyType)
                    {
                        prop.PropertyObject = obj;
                    }
                    else
                    {
                        throw new System.ArgumentException("The type of the property and the type of the object don't match.");
                    }
                }

            }

            //Collect the xml-summaries of the properties:
            if (typeFilePath != null)
            {
                PropertyCollectorDescription descCollector = new PropertyCollectorDescription();
                typeProperties = descCollector.collectPropertyDescription(typeFilePath, typeProperties);
            }

            //Merge typeProperties with PropertyDictionary. Entries with the same key will be overwritten.
            typeProperties.ToList().ForEach(x => PropertyDictionary[x.Key] = x.Value);
        }

        ///<summary>
        ///This method collects all properties from a type. It uses recursion to get the
        ///rest of the properties from all nested types if searchNestedClasses is true.       
        ///</summary>
        private Dictionary<string, Property> collectProperties(Type type, List<string> classesToCallList, bool searchNestedClasses, Dictionary<string, Property> typeProperties)
        {
            //Copy the list for the next recursive call of this method
            List<string> classesToCallNext = new List<string>();
            classesToCallNext = classesToCallList.ToList();
            //Add the name of the current type to the list 
            classesToCallNext.Add(type.Name);

            PropertyInfo[] properties = type.GetProperties();
            //Save every Property, of the type that was given to the method, to PropertyKeyList.
            foreach (PropertyInfo propInfo in properties)
            {
                string propertyName = propInfo.Name;
                string propertyClassName = type.Name;
                List<string> classesToCall = new List<string>(baseClasses.Concat(classesToCallNext)); 
                string propertyCanonicalName = SequenceToCallProperty(classesToCall, propertyName); //key for our dictionary

                typeProperties.Add(propertyCanonicalName, (new Property(propertyName, propertyClassName, propertyCanonicalName, propInfo)));
            }

            //End the collection of properties if SearchNestedClasses is set to false.
            if (!searchNestedClasses)
            {
                return typeProperties;
            }

            Type[] nestedTypes = type.GetNestedTypes();
            //Recursion: Use this method to find properties for every nested type.
            foreach (Type nestedType in nestedTypes)
            {
                collectProperties(nestedType, classesToCallNext, searchNestedClasses, typeProperties);
            }

            return typeProperties;
        }

        /// <summary>
        /// This method collects all names of the base classes of a type.
        /// Type TestCar.Car.Interior has Car and TestCar as base classes for example.
        /// </summary>
        private void collectBaseClasses(Type type)
        {            
            if (type.DeclaringType == null || baseClasses.Contains(type.DeclaringType.Name))
            {
                return;
            }
            else
            {
                baseClasses.Add(type.DeclaringType.Name);
                collectBaseClasses(type.DeclaringType);               
            }
        }
        
        ///<summary>
        ///Method returns a string which resembles the sequence to call a property.
        ///</summary>
        private string SequenceToCallProperty(List<string> classesToCallList, string propertyName)
        {
            string returnString = null;

            foreach (string classToCall in classesToCallList)
            {
                if (returnString == null)
                {
                    returnString = classToCall;
                }
                else
                {
                    returnString = returnString + "." + classToCall;
                }
            }
            return returnString + "." + propertyName;
        }
    }

    public class PropertyProcessor
    {
        private Dictionary<string, Property> properties = null;

        public PropertyProcessor(Dictionary<string, Property> properties)
        {
            this.properties = properties;
        }

        /// <summary>
        /// Prints all properties inside the console. 
        /// </summary>
        public void PrintProperties()
        {
            foreach (Property property in properties.Values)
            {
                Console.WriteLine(property.PropertyClassName + "\t" + property.PropertyName + "\t" + property.PropertyCanonicalName + "\t" + property.PropertyValue + "\t" + property.PropertyDescription);
            }
        }

        /// <summary>
        ///This method creates a csv file which has a line for every property we collected.
        ///Every line will have the class of the property, its name, its call sequence, its type and its value and description if available. 
        /// </summary>
        public void CsvPropertyListGenerator(string saveFolderPath)
        {
            using (StreamWriter outputFile = new StreamWriter(saveFolderPath + @"\PropertiesReflection.csv"))
            {
                outputFile.WriteLine("sep=,"); // make Excel use comma as field separator
                foreach (Property property in properties.Values)
                {
                    outputFile.WriteLine("{0},{1},{2},{3},{4},{5}", property.PropertyClassName, property.PropertyName, property.PropertyCanonicalName, property.PropertyType, property.PropertyValue, property.PropertyDescription);
                }
            }
        }
    }

    ///<summary>
    ///This program uses reflection to extract all Properties from a type or
    ///an object. It can also extract properties of nested classes. These
    ///properties will be saved in a dictionary from where they can be called
    ///via their canonical name. If object properties are collected, the canonical
    ///name can also be used to retrieve property values. If the .cs-File of the 
    ///type is available it can be used to extract the xml-summary of a property 
    ///via the use of Roslyn as well. The PropertyProcessor class includes methods 
    ///to print all Properties and a method to write all properties to a csv file.
    ///
    /// All in all this program can be useful when working with classes with a large
    /// number of properties.
    ///</summary>
    class PropertyCollectorReflection
    {
        static void Main(string[] args)
        {
            //Test: Collect all properties of a type and its nested types with their xml-summary.
            Console.WriteLine("################################ TEST ONE ################################" + "\n");
            
            //Collect all properties from a Type:
            //////////////EDIT THIS//////////////            
            Type type = typeof(TestCar);
            /////////////////////////////////////            

            //There is an optional setting to collect all property xml-summaries via Roslyn.
            //For this it's necessary to have the .cs-File which contains the class of the Type. 
            //////////////EDIT THIS////////////// 
            string typeFilePath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + @"\TestCar.cs";
            /////////////////////////////////////
            
            //Collect all properties:
            Properties properties = new Properties();
            ///////EDIT THIS (if needed)///////// 
            properties.addTypeProperties(type, true, typeFilePath: typeFilePath);
            /////////////////////////////////////
            var propertyDict = properties.PropertyDictionary;
            
            //Process all properties:
            PropertyProcessor propProcessor = new PropertyProcessor(propertyDict);

            //Print all properties: 
            propProcessor.PrintProperties();

            //Create a csv file which contains a list of all properties:
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            propProcessor.CsvPropertyListGenerator(desktopPath);

            //Test: Collect all properties of three objects and get their xml-summary.
            Console.WriteLine("\n" + "################################ TEST TWO ################################" + "\n");

            TestCar.Car car = new TestCar.Car("Audi", 123, true);
            TestCar.Car.Interior interior = new TestCar.Car.Interior(5, 2);
            TestCar.Car.Exterior exterior = new TestCar.Car.Exterior(4);

            //Collect all properties
            Properties properties2 = new Properties();
            properties2.addObjectProperties(car, typeFilePath: typeFilePath);
            properties2.addObjectProperties(interior, typeFilePath: typeFilePath);
            properties2.addObjectProperties(exterior, typeFilePath: typeFilePath);
            var propertyDict2 = properties2.PropertyDictionary;

            //Process all properties:
            PropertyProcessor propProcessor2 = new PropertyProcessor(propertyDict2);

            //Print all properties: 
            propProcessor2.PrintProperties();

            //Create a csv file which contains a list of all properties:                     
            //propProcessor2.CsvPropertyListGenerator(desktopPath);

            Console.WriteLine("\n" + "################ GET PROPERTY VALUE VIA ITS CANONICAL NAME ################" + "\n");

            Property numberSeats = propertyDict2["TestCar.Car.Interior.NumberSeats"];
            Console.WriteLine(numberSeats.PropertyName + " " + numberSeats.PropertyValue);
        }
    }
}