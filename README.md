<b>Combines Office OpenXMLPowerTools with HtmlToOpenXML</b> 

<b>Problem Statement</b>
   
   The Current OpenXMLTools Document Assembler doesn't have feature to replace html tags within data with Word formatted strings. 
   On of the client project has requirements to enter product information such as name, description, price and features through a web      portal and generate the pre-defined structured reports with those information. The product decription and features section in the        portal, is having WYSIWYG Editors. So OpenXMLPowerTools was not rendering those html tags in the generated reports. So there a need      the way to format the information.
   
<b>Solution</b>

   Combine the OpenXMLPowerTools and a library called HtmlToOpenXML in a way that those html tags can be formatted in the generated document.

<b>Build Instructions</b>

  Prerequisites: Visual Studio 2017 Update 5 or .NET CLI toolchain

<b>Build</b>

  <i>With Visual Studio:</i>

   Open OpenXmlPowerTools.sln in Visual Studio
   Rebuild the projects
   Build the solution.
   To run an example, set the OpenXMLPowerToolTest as the startup project, and press F5.

  <i>With .NET CLI toolchain:</i>

   Run dotnet build OpenXmlPowerTools.sln
   
 <b>Template Example</b>
 
   ExampleTemplates folder contains the example template (.dotx) to test. To make any Content Control as html formatted add attribute          Html="true". 
   For example <pre>&lt;Content Select="./Test" Html="true"/&gt;</pre>
