//
// AccountLoginForm.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Mono.Unix;
using Gtk;

using Lastfm;

namespace Lastfm.Gui
{
    public class AccountLoginForm : Gtk.Table
    {
        private Account account;
        private Entry username_entry;
        private LinkButton signup_button;
        private Button authorize_button;

        private bool save_on_edit = false;

        public AccountLoginForm (Account account) : base (1, 2, false)
        {
            this.account = account;

            BorderWidth = 5;
            RowSpacing = 5;
            ColumnSpacing = 5;

            Label username_label = new Label (Catalog.GetString ("Username:"));
            username_label.Xalign = 1.0f;
            username_label.Show ();

            username_entry = new Entry ();
            username_entry.Show ();

            Attach (username_label, 0, 1, 0, 1, AttachOptions.Fill,
                AttachOptions.Shrink, 0, 0);

            Attach (username_entry, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand,
                AttachOptions.Shrink, 0, 0);

            username_entry.Text = account.UserName ?? String.Empty;
        }

        protected override void OnDestroyed ()
        {
            if (save_on_edit) {
                Save ();
            }

            base.OnDestroyed ();
        }

        public void AddSignUpButton ()
        {
            if (signup_button != null) {
                return;
            }

            Resize (2, 2);
            signup_button = new LinkButton (account.SignUpUrl, Catalog.GetString ("Sign up for Last.fm"));
            signup_button.Show ();
            Attach (signup_button, 1, 2, 1, 2, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
        }

        public void AddAuthorizeButton ()
        {
            if (authorize_button != null) {
                return;
            }

            Resize (3, 2);
            authorize_button = new Button (Catalog.GetString ("Authorize for Last.fm"));
            authorize_button.Clicked += OnAuthorize;
            authorize_button.Show ();
            Attach (authorize_button, 1, 2, 2, 3, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);
        }

        private void OnAuthorize (object o, EventArgs args)
        {
            account.SessionKey = null;
            account.RequestAuthorization ();
        }

        public void Save ()
        {
            bool is_modified = false;

            if (account.UserName != username_entry.Text.Trim ()) {
                account.UserName = username_entry.Text.Trim ();
                is_modified = true;
            }
            if (account.SessionKey == null) {
                account.FetchSessionKey ();
                is_modified = true;
            }

            if (is_modified) {
                account.Save ();
            }
        }

        public bool SaveOnEdit {
            get { return save_on_edit; }
            set { save_on_edit = value; }
        }

        public string Username {
            get { return username_entry.Text; }
        }
    }
}

