﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;

namespace Examples
{
    public partial class ExampleLauncher : Form
    {
        public ExampleLauncher()
        {
            InitializeComponent();
        }

        Dictionary<string, Thread> executingExamples = new Dictionary<string, Thread>();

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                Type example =
                    Assembly.GetExecutingAssembly().GetType(
                        "Examples." + listBox1.SelectedItem.ToString().Replace(": ", "."),
                        true,
                        true
                    );
                example.InvokeMember("Launch", BindingFlags.InvokeMethod, null, null, null);
                /*
                if (!executingExamples.ContainsKey(listBox1.SelectedItem.ToString()))
                {
                    Thread newExample = new Thread(
                        new ParameterizedThreadStart(
                            this.Invoke
                        )
                    );
                    executingExamples.Add(listBox1.SelectedItem.ToString(), newExample);
                    newExample.Start();
                }*/
            }
        }

        public void ExampleLauncher_Load(object sender, EventArgs e)
        {
            // Get all examples
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                if (type.IsPublic)
                {
                    MemberInfo[] runMethods = type.GetMember("Launch");
                    foreach (MemberInfo run in runMethods)
                    {
                        // Trim the 'Examples.' namespace.
                        listBox1.Items.Add(
                            type.Namespace.Replace("Examples.", String.Empty) + ": " + type.Name
                        );
                    }
                }
            }

            // Select first item
            if (listBox1.Items.Count > 0)
            {
                this.listBox1.SelectedIndex = 0;
            }
        }
    }
}