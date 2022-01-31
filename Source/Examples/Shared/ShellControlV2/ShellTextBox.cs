// ---------------------------------------------------------------------
// Copyright (c) 2005 S. Senthil Kumar
// http://www.codeproject.com/Articles/9621/ShellControl-A-console-emulation-control
// ---------------------------------------------------------------------
using System.Windows.Forms;
namespace ShellControlV2
{
    public partial class ShellTextBox : TextBox
    {
        private string _prompt = ">>>";
        private readonly CommandHistory _commandHistory = new CommandHistory();

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellTextBox"/> class.
        /// </summary>
        public ShellTextBox()
        {
            InitializeComponent();
            CommandCompletion = new CommandCompletion();
            KeyPress += shellTextBox_KeyPress;
            KeyDown += ShellControl_KeyDown;
            KeyUp += OnKeyUp;
            PreviewKeyDown += PreviewKeyDown_event;

            PrintPrompt();
        }

        public CommandCompletion CommandCompletion { get; set; }

        public string Prompt
        {
            get => _prompt;
            set => SetPromptText(value);
        }

        public string[] GetCommandHistory()
        {
            return _commandHistory.GetCommandHistory();
        }

        public void WriteText(string text)
        {
            AddText(text);
        }

        public void WriteCommand(string text)
        {
            PrintPrompt();
            AddText(text);

            CommandCompletion.Reset();
            var currentCommand = GetTextAtPrompt();
            if (currentCommand.Length != 0)
            {
                ((ShellControl)Parent).FireCommandEntered(currentCommand);
                _commandHistory.Add(currentCommand);
            }
        }

        // Overridden to protect against deletion of contents
        // cutting the text and deleting it from the context menu
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x0302: //WM_PASTE
                case 0x0300: //WM_CUT
                case 0x000C: //WM_SETTEXT
                    if (!IsCaretAtWritablePosition())
                        MoveCaretToEndOfText();
                    break;
                case 0x0303: //WM_CLEAR
                    return;
            }
            base.WndProc(ref m);
        }

        // Handle Backspace and Enter keys in KeyPress. A bug in .NET 1.1
        // prevents the e.Handled = true from having the desired effect in KeyDown
        private void shellTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Handle backspace
            if (e.KeyChar == (char)8 && IsCaretJustBeforePrompt())
            {
                e.Handled = true;
                return;
            }

            // Handle backspace
            if (e.KeyChar == (char)8)
            {
                CommandCompletion.Reset();
                return;
            }

            if (!IsTerminatorKey(e.KeyChar)) return;

            e.Handled = true;
            CommandCompletion.Reset();
            string currentCommand = GetTextAtPrompt();
            if (currentCommand.Length != 0)
            {
                PrintLine();
                ((ShellControl)Parent).FireCommandEntered(currentCommand);
                _commandHistory.Add(currentCommand);
            }
            PrintPrompt();
        }

        private void ShellControl_KeyDown(object sender, KeyEventArgs e)
        {
            // If the caret is anywhere else, set it back when a key is pressed.
            if (!IsCaretAtWritablePosition() && !(e.Control || IsTerminatorKey(e.KeyCode)))
            {
                MoveCaretToEndOfText();
            }

            // Prevent caret from moving before the prompt
            if (e.KeyCode == Keys.Left && IsCaretJustBeforePrompt())
            {
                e.Handled = true;
            }
            else switch (e.KeyCode)
                {
                    case Keys.Down:
                        if (_commandHistory.DoesNextCommandExist())
                        {
                            ReplaceTextAtPrompt(_commandHistory.GetNextCommand());
                        }
                        e.Handled = true;
                        break;
                    case Keys.Up:
                        if (_commandHistory.DoesPreviousCommandExist())
                        {
                            var text = _commandHistory.GetPreviousCommand();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                ReplaceTextAtPrompt(text);
                            }
                        }
                        e.Handled = true;
                        break;
                    case Keys.Right:
                        // Performs command completion
                        string currentTextAtPrompt = GetTextAtPrompt();
                        string lastCommand = _commandHistory.LastCommand;

                        if (lastCommand != null &&
                            (currentTextAtPrompt.Length == 0 || lastCommand.StartsWith(currentTextAtPrompt)))
                        {
                            if (lastCommand.Length > currentTextAtPrompt.Length)
                            {
                                AddText(lastCommand[currentTextAtPrompt.Length].ToString());
                            }
                        }
                        break;
                    default:
                        if (e.Shift && e.KeyCode == Keys.Delete)
                        {
                            e.Handled = true;
                        }
                        else if (e.KeyCode == Keys.Tab)
                        {
                            e.Handled = true;
                            ReplaceTextAtPrompt(CommandCompletion.Complete(GetTextAtPrompt()));
                        }
                        break;
                }
        }

        private void PreviewKeyDown_event(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
                e.IsInputKey = true;
        }

        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.KeyCode != Keys.Tab) return;
            var data = GetTextAtPrompt();
            if (data.Length > 0 && data[data.Length - 1] == char.Parse("\t"))
            {
                ReplaceTextAtPrompt(data.Substring(0, data.Length - 1));
            }
        }

        private void PrintPrompt()
        {
            var currentText = Text;
            if (currentText.Length != 0 && currentText[currentText.Length - 1] != '\n')
                PrintLine();
            AddText(_prompt);
        }

        public void PrintLine()
        {
            AddText(System.Environment.NewLine);
        }

        private string GetCurrentLine()
        {
            if (Lines.Length > 0)
            {
                return (string)Lines.GetValue(Lines.GetLength(0) - 1);
            }
            return "";
        }

        private string GetTextAtPrompt()
        {
            return GetCurrentLine().Substring(_prompt.Length);
        }

        private void ReplaceTextAtPrompt(string text)
        {
            var currentLine = GetCurrentLine();
            int charactersAfterPrompt = currentLine.Length - _prompt.Length;

            if (charactersAfterPrompt == 0)
            {
                AddText(text);
            }
            else
            {
                Select(TextLength - charactersAfterPrompt, charactersAfterPrompt);
                SelectedText = text;
            }
        }

        private bool IsCaretAtCurrentLine()
        {
            return TextLength - SelectionStart <= GetCurrentLine().Length;
        }

        private void MoveCaretToEndOfText()
        {
            SelectionStart = TextLength;
            ScrollToCaret();
        }

        private bool IsCaretJustBeforePrompt()
        {
            return IsCaretAtCurrentLine() && GetCurrentCaretColumnPosition() == _prompt.Length;
        }

        private int GetCurrentCaretColumnPosition()
        {
            var currentLine = GetCurrentLine();
            var currentCaretPosition = SelectionStart;
            return currentCaretPosition - TextLength + currentLine.Length;
        }

        private bool IsCaretAtWritablePosition()
        {
            return IsCaretAtCurrentLine() && GetCurrentCaretColumnPosition() >= _prompt.Length;
        }

        private void SetPromptText(string val)
        {
            Select(0, _prompt.Length);
            SelectedText = val;
            _prompt = val;
        }

        private bool IsTerminatorKey(Keys key)
        {
            return key == Keys.Enter;
        }

        private bool IsTerminatorKey(char keyChar)
        {
            return keyChar == 13;
        }

        // Substitute for buggy AppendText()
        private void AddText(string text)
        {
            var newText = text.Replace("\t", "");
            Text += newText;
            MoveCaretToEndOfText();
        }
    }
}
