# SAS custom task example: System Command runner
***
This repository contains one of a series of examples that accompany
_Custom Tasks for SAS Enterprise Guide using Microsoft .NET_ 
by [Chris Hemedinger](http://support.sas.com/hemedinger).

This particular example goes with
**Chapter 14: Take Command with System Commands**.  There are two versions of the example: one 
built using C# and one built using Visual Basic .NET 
(both in Microsoft Visual Studio 2010).  It should run in SAS Enterprise Guide 4.2 and later.

## About this example
This task example is a process that doesn't create or run a SAS program. In fact, 
it doesn't interact with your SAS process at all. It's conceptually equivalent to a 
Windows batch file that runs at an appointed location in your SAS Enterprise Guide 
process flow. You can use it to copy files, create directories, or even launch 
another process.

The task implements the **ISASTaskExecution** interface, which tells the 
host application that it will run itself.

You can learn more about using the System Command example by 
[reading this blog post](http://blogs.sas.com/content/sasdummy/2007/10/05/you-are-under-my-command-prompt/).

