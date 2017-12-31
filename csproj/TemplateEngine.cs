using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Management.Automation;

namespace Horker.TemplateEngine
{
    public class TemplateSyntaxErrorException : Exception
    {
        public TemplateSyntaxErrorException(int lineNumber)
        : base(String.Format("premature end of code block at line {0}", lineNumber))
        {
        }
    }

    public class ScriptGenerator
    {
        private class EndOfInputException : Exception
        {
            public EndOfInputException()
            {
            }
        }

        string _template;
        int _pos;
        int _lineNumber;
        StringBuilder _output;
        bool _withinCode;

        public ScriptGenerator()
        {
        }

        private char GetNextChar()
        {
            if (_pos == _template.Length) {
                throw new EndOfInputException();
            }

            var ch = _template[_pos];
            ++_pos;

            if ((ch == '\n') || (ch == '\r' && Peek() != '\n')) {
                ++_lineNumber;
            }

            return ch;
        }

        private void PushBackChar()
        {
            --_pos;
        }

        private bool hasNextChar()
        {
            return _pos < _template.Length;
        }

        private char Peek()
        {
            if (_pos >= _template.Length) {
                return '\0';
            }
            return _template[_pos];
        }

        private void Append(char ch)
        {
            _output.Append(ch);
        }

        private void OpenLiteral()
        {
            _output.Append(";'");
            _withinCode = false;
        }

        private void CloseLiteral()
        {
            _output.Append(String.Format("'; [Horker.TemplateEngine.Runtime]::LineNumber = {0};", _lineNumber));
            _withinCode = true;
        }

        private void EmitProlog()
        {
        }

        private void EmitEpilog()
        {
        }

        public string GenerateScript(string template)
        {
            _template = template;
            _output = new StringBuilder((int)(_template.Length * 1.5));
            _withinCode = false;
            _pos = 0;
            _lineNumber = 1;

            EmitProlog();

            try {
                OpenLiteral();

                Char ch;
                for (;;) {
                    ch = GetNextChar();
                    if (ch == '<') {
                        ch = GetNextChar();
                        if (ch == '%') {
                            CloseLiteral();
                            for (;;) {
                                ch = GetNextChar();
                                if (ch == '%') {
                                    ch = GetNextChar();
                                    if (ch == '>') {
                                        OpenLiteral();
                                        break;
                                    }
                                    else {
                                        Append('%');
                                        Append(ch);
                                    }
                                }
                                else if (ch == '-') {
                                    ch = GetNextChar();
                                    if (ch == '%') {
                                        ch = GetNextChar();
                                        if (ch == '>') {
                                            // It doesn't show the following newlines.
                                            // To keep the line numbers of the template correct,
                                            // newlines are output as part of the processing script.
                                            // These should be done before OpenLiteral().
                                            if (Peek() == '\n') {
                                                ch = GetNextChar();
                                                Append(ch);
                                            }
                                            else if (Peek() == '\r') {
                                                ch = GetNextChar();
                                                Append(ch);
                                                if (Peek() == '\n') {
                                                    ch = GetNextChar();
                                                    Append(ch);
                                                }
                                            }
                                            OpenLiteral();
                                            break;
                                        }
                                        else {
                                            Append('-');
                                            Append('%');
                                            Append(ch);
                                        }
                                    }
                                    else {
                                        Append('-');
                                        Append(ch);
                                    }
                                }
                                else {
                                    Append(ch);
                                }
                            }

                        }
                        else {
                            Append('<');
                            Append(ch);
                        }
                    }
                    else {
                        Append(ch);
                    }
                }
            }
            catch (EndOfInputException)
            {
                // pass through
            }

            if (_withinCode) {
                throw new TemplateSyntaxErrorException(Runtime.LineNumber);
            }

            CloseLiteral();
            EmitEpilog();

            return _output.ToString();
        }
    }

    public class Runtime
    {
        static private ThreadLocal<int> _lineNumber = new ThreadLocal<int>(()=> { return 1; });

        static public int LineNumber
        {
            get { return _lineNumber.Value; }
            set { _lineNumber.Value = value; }
        }
    }

    /// <summary>
    /// <para type="synopsis">Process a PowerShell-based document template and generates a document dynamically.</para>
    /// <para type="description">Invoke-TemplateEngine processes a document template that embeds PowerShell scripts and generates a plain text as an outcome to the standard output stream.</para>
    /// <para type="description">It recognizes text portions enclosed with &lt;% and %&gt; as PowerShell code snippets and executes them. The objects returned by the code into the standard output stream are written into a resultant document.</para>
    /// <para type="description">Text portions enclosed with &lt;% and- %&gt; are processed as the same above, but the following newlines will not appear in the output.</para>
    /// <para type="description">Note that, unlike usual PowerShell output, such objects are converted into strings by the ToString() method and no newlines are inserted between them. See Example section for details.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// PS&gt;Get-Content template.txt
    /// Dear &lt;% $customer %&gt;,
    /// This is a mail for you.
    /// PS&gt; 
    /// PS&gt;$customer = "Bill"
    /// PS&gt;Get-Content template.txt | Invoke-TemplateEngine
    /// Dear Bill,
    /// This is a mail for you.
    /// PS&gt; 
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// PS&gt;Invoke-TemplateEngine '&lt;% foreach ($i in 1..3) { $i } %&gt;'
    /// 123
    /// </code>
    /// <para>The object returned from the script are written into the output without any gaps. To control spaces or newlines between them, see the next example.</para>
    /// <para></para>
    /// </example>
    /// <example>
    /// <code>
    /// PS&gt;Get-Content template.txt
    /// &lt;% foreach ($i in 1..3) { -%&gt;
    /// &lt;%   $i %&gt;
    /// &lt;% } -%&gt;
    /// PS&gt;
    /// PS&gt;Get-Content template.txt | Invoke-TemplateEngine
    /// 1
    /// 2
    /// 3
    /// </code>
    /// <para>When '-%&gt;' is used as a closing tag, a following newline of a code block will not appear in the output.</para>
    /// <para></para>
    /// </example>
    /// <example>
    /// <code>
    /// PS&gt;dir C:\work
    ///
    ///     Directory: C:\work
    ///
    /// Mode          LastWriteTime Length Name
    /// ----          ------------- ------ ----
    /// -a---- 2017/12/29     17:36    252 a.txt
    /// -a---- 2017/12/29     17:36    252 b.txt
    /// -a---- 2017/12/29     17:36    252 c.txt
    ///
    /// PS&gt;##### Objects are stringified by the ToString() method.
    /// PS&gt;
    /// PS&gt;Invoke-TemplateEngine '&lt;% dir %&gt;'
    /// a.textb.txtc.txt
    /// PS&gt;
    /// PS&gt;##### Specify a property name to manage output format
    /// PS&gt;
    /// PS&gt;Invoke-TemplateEngine '&lt;% (dir).FullName -join "`r`n" %&gt;'
    /// C:\work\a.txt
    /// C:\work\b.txt
    /// C:\work\c.txt
    /// PS&gt;
    /// PS&gt;##### Format-Table and Out-String enable to generate host-like output.
    /// PS&gt;
    /// PS&gt;Invoke-TemplateEngine '&lt;% dir | ft -auto | out-string %&gt;'
    ///
    ///     Directory: C:\work
    ///
    /// Mode          LastWriteTime Length Name
    /// ----          ------------- ------ ----
    /// -a---- 2017/12/29     17:36    252 a.txt
    /// -a---- 2017/12/29     17:36    252 b.txt
    /// -a---- 2017/12/29     17:36    252 c.txt
    /// </code>
    /// </example>
    [Cmdlet("Invoke", "TemplateEngine")]
    public class InvokeTemplateEngine : PSCmdlet
    {
        /// <summary>
        /// <para type="description">A document template, given as a string or an array of strings through pipeline.</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public string Template { get; set; }

        /// <summary>
        /// <para type="description">If specified, an internal script for processing will be saved into the file. (For debugging purpse)</para>
        /// </summary>
        [Parameter(Position = 1, Mandatory = false)]
        public string ProcessorFile { get; set; } = "";

        private StringBuilder _buffer;

        protected override void BeginProcessing()
        {
            _buffer = new StringBuilder();
        }

        protected override void ProcessRecord()
        {
            _buffer.Append(Template);
            _buffer.Append("\r\n");
        }

        protected override void EndProcessing()
        {
            var template = _buffer.ToString();

            var generator = new ScriptGenerator();

            string script = "";
            try {
                script = generator.GenerateScript(template);
            }
            catch (TemplateSyntaxErrorException e) {
                WriteError(new ErrorRecord(e, "", ErrorCategory.WriteError, null));
            }

            if (ProcessorFile != "") {
                File.WriteAllText(ProcessorFile, script, Encoding.UTF8);
            }

            _buffer.Clear();
            using (var invoker = new RunspaceInvoke()) {
                IList errors;
                var results = invoker.Invoke(script, new object[0], out errors);
                foreach (var obj in results) {
                    _buffer.Append(obj.ToString());
                }
            }

            WriteObject(_buffer.ToString());
        }
    }
}
