
# Introduction

SharpMeter is a library to communicate with meter using PSEM or COSM.


  

# Getting Started

- Direct usage by using SharpMeter.dll and adding it as reference in VS
 *- [WIP] Exposed COM functionality*
- Load the entire solution and use "Test Meter" project


  

# Build and Test

Open solution via VS and build it

  

# Status

**- PSEM [C12]:**

	- Procedures: COMPLETED

	- Identity, Negotiate, Logon, Logoff, Terminate, Wait, Security and other **C12.8** 	Requests and responses

	- Table de-serialization using JSON from hex/bytes to fields

	- Read and write
	
	- Ability to save meter object to file and restore it along with its data
		

**- COSM [IEC 62056]:**

	- Design: WIP and in parallel with implementation
	- HDLC (Optical Mode E): 
		- SNRM - Done
		- AARQ/AARE - Done
		- Get all objects - WIP
			- Create class to represents each object
			- A list to hold all object
		- COSM meter class - WIP

		
  
  
<h2>💻 Built with</h2>

Technologies used in the project:

*   C#

<h2>🛡️ License:</h2>

This project is licensed under the MIT

# Future Plans
-   GUI for PSEM and COSM
-	Meter patching.
-	TCP/IP connectivity support.
-	CLI system to invoke certain functionalities of PSEM or COSM.
-	Possible AMI devices cold start/reset functionality for each one.
