QuickLaunch
==============

Open your favorite files, URLs or programs with just a few keystrokes!

Bring up the interface pressing `Windows+Escape`, the launch bar will appear on the bottom right of your screen.

According to your configuration, with a few keystrokes you will be able to execute actions like opening a file.
E.g. pressing `Q` you might open some file, `2Q` might open some other file, or `U` might open some URL in your browser.


Configuration
----------------

Open the configuration by pressing the gears button on the launch bar (or use the default action by pressing `Ctrl+F5`). 
The configuration panel will appear.

On the left side of the panel, you'll see a list of commands, while on the right side you can configure dispatchers.

*Command*s are a combination of a keystroke sequence and an associated *Dispatcher* to invoke when this sequence
is entered by the user, along with an action index. This action index can be entered before the command's key sequence, and it usually defaults to 1.  
For example you might have a command defined with keystroke `Q` invoking dispatcher QuickOpen. If you just press `Q`, the
dispatcher QuickOpen will be invoked with index `1` (default). However, if you press `2Q` it will invoke the dispatcher
with action index `2`.

*Dispatcher*s are a container for launching specific *Action*s according to the index used in their invocation.
These actions can be freely assigned, however it could make sense to assign actions of the same type to a dispatcher.  
For example dispatcher *QuickOpen* might be defined like this:  
	1 → Open file `foo.txt` (i.e. *OpenFile* action with path *foo.txt*)  
	2 → Open file `bar.txt` (i.e. *OpenFile* action with path *bar.txt*)

As a summary, this is the sequence of events involved in executing an action:

	User enters key sequence → Command is triggered → Invoke dispatcher with action index → Execute action

The definition of dispatchers on the right side of the configuration panel is currently rather quirky.

