using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PropertyCollector
{
    public class PropertyWalker : CSharpSyntaxWalker
    {
        private List<string> classesToCallList = new List<string>();
        private string classNodeName;
        private string parentClassNodeName = null;

        public Dictionary<string, Property> PropertyDictionary { get; } = new Dictionary<string, Property>();

        public PropertyWalker(Dictionary<string, Property> propertyDictionary)
        {
            this.PropertyDictionary = propertyDictionary;
        }

        /// <summary>
        ///Class Visitor
        /// </summary>
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            classNodeName = node.Identifier.ToString();

            //We have to get the ParentClassNode to build a method call for a property
            if (node.Parent.Kind().ToString() == "ClassDeclaration")
            {
                var parentClassNode = (ClassDeclarationSyntax)node.Parent;
                parentClassNodeName = parentClassNode.Identifier.ToString();
                ClassesToCallProperty(parentClassNodeName, classNodeName);
            }
            //Inital State: Parent of class is namespace or there is no prior code at all.
            else
            {
                ClassesToCallProperty(null, classNodeName);
            }

            base.VisitClassDeclaration(node);
        }

        /// <summary>
        ///Property Visitor
        /// </summary>
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            string propertyName = node.Identifier.ToString();
            string propertyCanonicalName = SequenceToCallProperty(classesToCallList, propertyName); //key for our dictionary            
            string propertySummary = GetXMLSummary(node.GetLeadingTrivia());

            //Add summary to the respective Property inside PropertyDictionary:
            if (PropertyDictionary.ContainsKey(propertyCanonicalName))
            {
                PropertyDictionary[propertyCanonicalName].PropertyDescription = propertySummary;
            }
            
            base.VisitPropertyDeclaration(node);
        }

        /// <summary>
        ///Method that maintains a list with class names in the order how they have to be called to get a Property.
        /// </summary>
        public void ClassesToCallProperty(string parentClassNodeName, string classNodeName)
        {
            if (!classesToCallList.Any())
            {
                classesToCallList.Add(parentClassNodeName);
            }

            //If the class parent node of a class node already exists in a list, then everything after this node
            //should be deleted from the list. After this a new class node can be added to the list.
            if (classesToCallList.Contains(parentClassNodeName))
            {
                int afterParentIndex = classesToCallList.IndexOf(parentClassNodeName) + 1;
                int range = classesToCallList.Count - afterParentIndex;
                classesToCallList.RemoveRange(afterParentIndex, range);
            }

            classesToCallList.Add(classNodeName);
        }

        /// <summary>
        ///Method returns a string which resembles the sequence to call a property. 
        /// </summary>
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

        /// <summary>
        ///Method to extract the Summary from the XML Documentation of a Property inside the SyntaxTree.
        ///To get the summary every Property must have an XML-Documentation of the form: &lt;summary&gt;&lt;/summary&gt;
        /// </summary>
        private string GetXMLSummary(SyntaxTriviaList trivias)
        {
            //Get XML-Trivia from Node
            var xmlCommentTrivia = trivias.FirstOrDefault(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia);
            var xml = xmlCommentTrivia.GetStructure();
            if (xml == null) { return ""; }
            string xmlString = xml.ToString();
            //Get summary from XML-String
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            XmlNodeList xnList = xmlDoc.DocumentElement.SelectNodes("/summary");
            string summary = xnList.Item(0).InnerText;
            //Edit Summary-String            
            summary = summary.Replace("///", "");
            summary = summary.Trim();

            return summary;
        }
    }

    public class PropertyCollectorDescription
    {
        /// <summary>
        /// This method searches for xml-summaries of properties of a type. 
        /// The .cs-File of the type is needed and its file path has to be given to the method.
        /// </summary>
        public Dictionary<string, Property> collectPropertyDescription(string typeFilePath, Dictionary<string, Property> propertyDictionary)
        {
            //Read .cs-File
            SyntaxTree tree;

            using (var stream = File.OpenRead(typeFilePath))
            {
                tree = CSharpSyntaxTree.ParseText(SourceText.From(stream), path: typeFilePath);
            }

            //Only collect Properties from the class of the Properties which are inside propertyDictionary.
            string startClassName = propertyDictionary.Values.First().PropertyClassName;
            //Get code snippet of the class from the .cs-File
            string codeSnippet = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Where(n => n.Identifier.ValueText == startClassName).First().ToString();
            var root = CSharpSyntaxTree.ParseText(codeSnippet).GetRoot();

            //Collect all property descriptions:
            var walker = new PropertyWalker(propertyDictionary);
            walker.Visit(root);

            return walker.PropertyDictionary;
        }
    }
}