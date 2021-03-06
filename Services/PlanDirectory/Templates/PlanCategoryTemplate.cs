﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 14.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

namespace HubWeb.Templates
{
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    
    #line 1 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "14.0.0.0")]
    public partial class PlanCategoryTemplate : PlanCategoryTemplateBase
    {
#line hidden
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write(@"<!DOCTYPE html>
<html>
<head>
    <link rel=""stylesheet"" href=""../bower_components/bootstrap/dist/css/bootstrap.min.css""/>
    <link rel=""stylesheet"" href=""../Content/metronic/components.css""/>

    <link href='https://fonts.googleapis.com/css?family=Francois+One' rel='stylesheet' type='text/css'>
    <link href='https://fonts.googleapis.com/css?family=Didact+Gothic' rel='stylesheet' type='text/css'>

    <link rel=""stylesheet"" href=""../Content/css/plan-category.css"" />
	<link rel=""stylesheet"" href=""../Content/css/plan-directory.css"" />

    <title></title>
    <meta charset=""utf-8""/>
</head>
<body
	 <div class=""header-container"">
        <div class=""header"">
            <div class=""logo""></div>                        
        </div>
    </div>
    <div class=""container"">        
        <p style=""font-size: 30px"">Plan Directory - ");
            
            #line 27 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(Name));
            
            #line default
            #line hidden
            this.Write("</p>\r\n        <div class=\"icons\">\r\n\t\t\t");
            
            #line 29 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
 int i = 0;
			foreach (var tag in Tags)
			{ 
            
            #line default
            #line hidden
            this.Write("\t\t\t<img class=\"web-service-icon\" src=\"..");
            
            #line 32 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(tag.Value));
            
            #line default
            #line hidden
            this.Write("\"/>\t\t\r\n\t\t\t\t");
            
            #line 33 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
 if (i < Tags.Count - 1)
				{ 
            
            #line default
            #line hidden
            this.Write("\t\t\t\t<img src=\"../Content/icons/plus.png\"/>\r\n\t\t\t\t");
            
            #line 36 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
 i++;
				} 
            
            #line default
            #line hidden
            this.Write("                        \r\n\t\t");
            
            #line 38 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write(@"        </div>
        <br/>
        <p style=""font-size: 15px"">Plan Definition Description</p>
        <p style=""font-size: 22px"">Related Plans</p>
            <table class=""table""> 
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Plan Name</th>
                        <th>Plan Description</th>
                        <th>Action</th>
                    </tr>
                </thead>
                <tbody>
                    ");
            
            #line 53 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
 int number = 1;
                    foreach (var plan in RelatedPlans) 
                    {
            
            #line default
            #line hidden
            this.Write("                    <tr>\r\n                        <th scope=\"row\">");
            
            #line 57 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(number++));
            
            #line default
            #line hidden
            this.Write("</th>\r\n                        <td>");
            
            #line 58 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(plan.Item1));
            
            #line default
            #line hidden
            this.Write("</td>\r\n                        <td>");
            
            #line 59 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(plan.Item2));
            
            #line default
            #line hidden
            this.Write("</td>\r\n                        <td><a href=\"");
            
            #line 60 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(plan.Item3));
            
            #line default
            #line hidden
            this.Write("\">Create</a></td>\r\n                    </tr>                   \r\n                " +
                    "    ");
            
            #line 62 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("                    \r\n                </tbody>\r\n            </table>\r\n</div>\r\n</b" +
                    "ody>\r\n</html>\r\n");
            return this.GenerationEnvironment.ToString();
        }
        
        #line 1 "C:\dev\Work\fr8company\CategoryPages\PlanCategoryTemplate.tt"

private string _NameField;

/// <summary>
/// Access the Name parameter of the template.
/// </summary>
private string Name
{
    get
    {
        return this._NameField;
    }
}

private global::System.Collections.Generic.IDictionary<System.String, System.String> _TagsField;

/// <summary>
/// Access the Tags parameter of the template.
/// </summary>
private global::System.Collections.Generic.IDictionary<System.String, System.String> Tags
{
    get
    {
        return this._TagsField;
    }
}

private global::System.Collections.Generic.IList<System.Tuple<System.String, System.String, System.String>> _RelatedPlansField;

/// <summary>
/// Access the RelatedPlans parameter of the template.
/// </summary>
private global::System.Collections.Generic.IList<System.Tuple<System.String, System.String, System.String>> RelatedPlans
{
    get
    {
        return this._RelatedPlansField;
    }
}


/// <summary>
/// Initialize the template
/// </summary>
public virtual void Initialize()
{
    if ((this.Errors.HasErrors == false))
    {
bool NameValueAcquired = false;
if (this.Session.ContainsKey("Name"))
{
    this._NameField = ((string)(this.Session["Name"]));
    NameValueAcquired = true;
}
if ((NameValueAcquired == false))
{
    object data = global::System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("Name");
    if ((data != null))
    {
        this._NameField = ((string)(data));
    }
}
bool TagsValueAcquired = false;
if (this.Session.ContainsKey("Tags"))
{
    this._TagsField = ((global::System.Collections.Generic.IDictionary<System.String, System.String>)(this.Session["Tags"]));
    TagsValueAcquired = true;
}
if ((TagsValueAcquired == false))
{
    object data = global::System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("Tags");
    if ((data != null))
    {
        this._TagsField = ((global::System.Collections.Generic.IDictionary<System.String, System.String>)(data));
    }
}
bool RelatedPlansValueAcquired = false;
if (this.Session.ContainsKey("RelatedPlans"))
{
    this._RelatedPlansField = ((global::System.Collections.Generic.IList<System.Tuple<System.String, System.String, System.String>>)(this.Session["RelatedPlans"]));
    RelatedPlansValueAcquired = true;
}
if ((RelatedPlansValueAcquired == false))
{
    object data = global::System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("RelatedPlans");
    if ((data != null))
    {
        this._RelatedPlansField = ((global::System.Collections.Generic.IList<System.Tuple<System.String, System.String, System.String>>)(data));
    }
}


    }
}


        
        #line default
        #line hidden
    }
    
    #line default
    #line hidden
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "14.0.0.0")]
    public class PlanCategoryTemplateBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        protected System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}
