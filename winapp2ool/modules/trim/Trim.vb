﻿'    Copyright (C) 2018-2024 Hazel Ward
'
'    This file is a part of Winapp2ool
'
'    Winapp2ool is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    Winapp2ool is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with Winapp2ool.  If not, see <http://www.gnu.org/licenses/>.
Option Strict On
Imports System.IO

'''    <summary>
'''    Checks the detection criteria of each entry in a winapp2.ini file against the current system 
'''    Any entries whose criteria do not match the current system are then removed from the final output file
'''   </summary>
'''   Docs last updated 2022-11-21
Public Module Trim
    ''' <summary> The winapp2.ini file that will be trimmed </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Property TrimFile1 As New iniFile(Environment.CurrentDirectory, "winapp2.ini", mExist:=True)

    ''' <summary> Holds the path of an iniFile containing the names of Sections who should never be trimmed </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Property TrimFile2 As New iniFile(Environment.CurrentDirectory, "includes.ini")

    ''' <summary> Holds the path where the output file will be saved to disk. Overwrites the input file by default </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Property TrimFile3 As New iniFile(Environment.CurrentDirectory, "winapp2.ini", "winapp2-trimmed.ini")

    ''' <summary> Holds the path of an iniFile containing the names of Sections who should always be trimmed </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Property TrimFile4 As New iniFile(Environment.CurrentDirectory, "excludes.ini")

    ''' <summary> The major/minor version number on the current system </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Property winVer As Double

    ''' <summary> Indicates that the module settings have been modified from their defaults </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Property TrimModuleSettingsChanged As Boolean = False

    ''' <summary> Indicates that we are downloading a winapp2.ini from GitHub </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Property DownloadFileToTrim As Boolean = False

    ''' <summary> Indicates that the includes should be consulted while trimming </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Property UseTrimIncludes As Boolean = False

    ''' <summary> Indicates that the excludes should be consulted while trimming </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Property UseTrimExcludes As Boolean = False

    ''' <summary> Handles the commandline args for Trim </summary>
    ''' Trim args:
    ''' -d          : download the latest winapp2.ini
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Sub handleCmdLine()

        initDefaultTrimSettings()
        handleDownloadBools(DownloadFileToTrim)
        getFileAndDirParams(TrimFile1, New iniFile, TrimFile3)
        initTrim()

    End Sub

    ''' <summary> Trims an <c> iniFile </c> from outside the module </summary>
    ''' <param name="firstFile"> The winapp2.ini file to be trimmed </param>
    ''' <param name="thirdFile"> <c> iniFile </c> containing the path on disk to which the trimmed file will be saved </param>
    ''' <param name="d"> Indicates that the input winapp2.ini should be downloaded from GitHub </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Sub remoteTrim(firstFile As iniFile, thirdFile As iniFile, d As Boolean)

        TrimFile1 = firstFile
        TrimFile3 = thirdFile
        DownloadFileToTrim = d
        initTrim()

    End Sub

    ''' <summary> Initiates the <c> Trim </c> process from the main menu or commandline </summary>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Sub initTrim()

        ' Don't try to trim an empty file 
        If Not DownloadFileToTrim Then If Not enforceFileHasContent(TrimFile1) Then Return

        ' Ensure we have an online connection before continuing if necessary 
        If DownloadFileToTrim Then If Not checkOnline() Then setHeaderText("Internet connection lost! Please check your network connection and try again", True) : Return

        Dim winapp2 = If(DownloadFileToTrim, New winapp2file(getRemoteIniFile(winapp2link)), New winapp2file(TrimFile1))

        ' Spin up our include/excludes 
        If UseTrimIncludes Then TrimFile2.validate()
        If UseTrimExcludes Then TrimFile4.validate()

        clrConsole()
        print(3, "Trimming... Please wait, this may take a moment...")

        Dim entryCountBeforeTrim = winapp2.count

        ' Perform the trim 
        trimFile(winapp2)

        ' Print trim summary to the user 
        clrConsole()
        print(4, "Trim Complete", conjoin:=True)
        print(0, "Entry Count", isCentered:=True, trailingBlank:=True)
        print(0, $"Initial: {entryCountBeforeTrim}")
        print(0, $"Trimmed: {winapp2.count}")
        Dim difference = entryCountBeforeTrim - winapp2.count
        print(0, $"{difference} entries trimmed from winapp2.ini ({Math.Round((difference / entryCountBeforeTrim) * 100)}%)")
        print(0, anyKeyStr, leadingBlank:=True, closeMenu:=True)
        gLog($"{difference} entries trimmed from winapp2.ini ({Math.Round((difference / entryCountBeforeTrim) * 100)}%)")
        gLog($"{winapp2.count} entries remain.")

        ' Save the trimmed file back to disk 
        TrimFile3.overwriteToFile(winapp2.winapp2string)

        ' If we downloaded the latest file, then we probably can mark winapp2 as having been updated 
        If DownloadFileToTrim Then waUpdateIsAvail = False
        setHeaderText($"{TrimFile3.Name} saved")

        crk()

    End Sub

    ''' <summary> Trims a <c> winapp2file </c>, removing entries not relevant to the current system </summary>
    ''' <param name="winapp2"> A <c> winapp2file </c> to be trimmed to fit the current system </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Public Sub trimFile(winapp2 As winapp2file)

        If winapp2 Is Nothing Then argIsNull(NameOf(winapp2)) : Return

        ' Winapp2.ini is composed of multiple entry lists representing the different top-level sections that we separate from the rest of the entries
        ' Pass them off individually and in-order for processing
        For i = 0 To winapp2.Winapp2entries.Count - 1
            Dim entryList = winapp2.Winapp2entries(i)
            processEntryList(entryList)
        Next

        winapp2.rebuildToIniFiles()
        winapp2.sortInneriniFiles()

    End Sub

    ''' <summary> Evaluates a <c> keyList </c> to observe whether they exist on the current machine </summary>
    ''' <param name="kl"> The <c> keyList </c> containing detection criteria to be evaluated </param>
    ''' <param name="chkExist"> The <c> function </c> that evaluates the detection criteria in <c> <paramref name="kl"/> </c> </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function checkExistence(ByRef kl As keyList, chkExist As Func(Of String, Boolean)) As Boolean

        ' If there's no keys then their content cannot exist 
        If kl.KeyCount = 0 Then Return False

        ' Process each key individually, if any exist return true 
        For Each key In kl.Keys

            If chkExist(key.Value) Then

                gLog($"{key.Value} matched a path on the system", Not kl.KeyType = "DetectOS", descend:=True, indent:=True, buffr:=True)
                Return True

            End If

        Next

        ' If we make it this far, no keys existed, so return false 
        Return False

    End Function

    ''' <summary> Audits the detection criteria in a given <c> winapp2entry </c> against the current system <br/> <br/>
    ''' Returns <c> True </c> if the detection criteria are met, <c> False </c> otherwise </summary>
    ''' <param name="entry"> A <c> winapp2entry </c> to whose detection criteria will be audited </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function processEntryExistence(ByRef entry As winapp2entry) As Boolean

        gLog($"Processing entry: {entry.Name}", ascend:=True, buffr:=True, leadr:=True)

        ' Respect the include/excludes 
        If UseTrimIncludes AndAlso TrimFile2.hasSection(entry.Name) Then gLog("Retaining entry: " & entry.Name, indent:=True, indAmt:=2, leadr:=True, buffr:=True) : Return True
        If UseTrimExcludes AndAlso TrimFile4.hasSection(entry.Name) Then gLog("Discarding entry: " & entry.Name, leadr:=True, indent:=True, buffr:=True) : Return False

        ' Process the DetectOS if we have one, take note if we meet the criteria, otherwise return false
        Dim hasMetDetOS = False

        If Not entry.DetectOS.KeyCount = 0 Then

            If winVer = Nothing Then winVer = getWinVer()

            hasMetDetOS = checkExistence(entry.DetectOS, AddressOf checkDetOS)
            gLog($"Met DetectOS criteria. {winVer} satisfies {entry.DetectOS.Keys.First.Value}", hasMetDetOS, indent:=True)
            gLog($"Did not meet DetectOS criteria. {winVer} does not satisfy {entry.DetectOS.Keys.First.Value}", Not hasMetDetOS, descend:=True, indent:=True)

            If Not hasMetDetOS Then Return False

        End If

        ' Process any other Detect criteria we have
        If checkExistence(entry.Detects, AddressOf checkRegExist) Then gLog("Retaining entry: " & entry.Name, indent:=True, leadr:=True, buffr:=True, indAmt:=2) : Return True
        If checkExistence(entry.DetectFiles, AddressOf checkPathExist) Then gLog("Retaining entry: " & entry.Name, indent:=True, leadr:=True, buffr:=True, indAmt:=2) : Return True
        If checkExistence(entry.SpecialDetect, AddressOf checkSpecialDetects) Then gLog("Retaining entry: " & entry.Name, indent:=True, leadr:=True, buffr:=True, indAmt:=2) : Return True

        ' Return true for the case where we have only a DetectOS and we meet its criteria
        Dim onlyHasDetOS = entry.SpecialDetect.KeyCount + entry.DetectFiles.KeyCount + entry.Detects.KeyCount = 0
        gLog("No other detection keys found than DetectOS", onlyHasDetOS AndAlso hasMetDetOS, descend:=True)
        If onlyHasDetOS AndAlso hasMetDetOS Then gLog("Retaining entry: " & entry.Name, leadr:=True, indent:=True, buffr:=True, indAmt:=2) : Return True

        ' Return true for the case where we have no valid detect criteria
        Dim hasNoDetectKeys = entry.DetectOS.KeyCount + entry.DetectFiles.KeyCount + entry.Detects.KeyCount + entry.SpecialDetect.KeyCount = 0
        gLog("No detect keys found, entry will be retained.", hasNoDetectKeys, descend:=True)
        If hasNoDetectKeys Then gLog("Retaining entry: " & entry.Name, leadr:=True, indent:=True, buffr:=True, indAmt:=2) : Return True

        gLog("Discarding entry: " & entry.Name, descend:=True, leadr:=True, buffr:=True, indent:=True)
        Return False

    End Function

    ''' <summary> Audits the given entry for legacy codepaths in the machine's VirtualStore </summary>
    ''' <param name="entry"> The <c> winapp2entry </c> to audit </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Sub virtualStoreChecker(ByRef entry As winapp2entry)

        gLog("Attempting to generate any neccessary VirtualStore keys for " & entry.Name, indent:=True, buffr:=True, ascend:=True)
        vsKeyChecker(entry.FileKeys)
        vsKeyChecker(entry.RegKeys)
        vsKeyChecker(entry.ExcludeKeys)
        gLog("VirtualStore audit complete ", leadr:=True, indent:=True, descend:=True)

    End Sub

    ''' <summary> Generates keys for VirtualStore locations that exist on the current system and inserts them into the given list </summary>
    ''' <param name="kl"> The <c> keyList </c> of FileKey, RegKey, or ExcludeKeys to be checked against the VirtualStore </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Sub vsKeyChecker(ByRef kl As keyList)

        If kl.KeyCount = 0 Then Return

        Dim starterCount = kl.KeyCount

        Select Case kl.KeyType

            Case "FileKey", "ExcludeKey"

                mkVsKeys({"%ProgramFiles%", "%CommonAppData%", "%CommonProgramFiles%", "HKLM\Software"}, {"%LocalAppData%\VirtualStore\Program Files*", "%LocalAppData%\VirtualStore\ProgramData", "%LocalAppData%\VirtualStore\Program Files*\Common Files", "HKCU\Software\Classes\VirtualStore\MACHINE\SOFTWARE"}, kl)

            Case "RegKey"

                mkVsKeys({"HKLM\Software"}, {"HKCU\Software\Classes\VirtualStore\MACHINE\SOFTWARE"}, kl)

        End Select

        If Not starterCount = kl.KeyCount Then kl.renumberKeys(replaceAndSort(kl.toStrLst(True), "|", " \ \"))

    End Sub

    ''' <summary> Creates <c> iniKeys </c> to handle VirtualStore locations that correspond to paths given in <c> <paramref name="kl"/> </c> </summary>
    ''' <param name="findStrs"> An array of Strings to seek for in the key value </param>
    ''' <param name="replStrs"> An array of strings to replace the sought after key values </param>
    ''' <param name="kl"> The <c> keylist </c> to be processed </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Sub mkVsKeys(findStrs As String(), replStrs As String(), ByRef kl As keyList)

        Dim initVals = kl.toStrLst(True)
        Dim keysToAdd As New keyList(kl.KeyType)

        For Each key In kl.Keys

            If Not key.vHasAny(findStrs, True) Then Continue For

            For i = 0 To findStrs.Length - 1

                Dim keyToAdd = createVSKey(findStrs(i), replStrs(i), key)

                ' Don't recreate keys that already exist
                If initVals.contains(keyToAdd.Value) Then Continue For

                keysToAdd.add(keyToAdd, Not key.Value = keyToAdd.Value)

            Next

        Next

        Dim kl2 = kl
        keysToAdd.Keys.ForEach(Sub(key) kl2.add(key, checkExist(New winapp2KeyParameters(key).PathString)))
        kl = kl2

    End Sub

    ''' <summary> Creates the VirtualStore version of a given <c> iniKey </c> </summary>
    ''' <param name="findStr"> The normal filesystem path </param>
    ''' <param name="replStr"> The VirtualStore path </param>
    ''' <param name="key"> The <c> iniKey </c> to processed into a VirtualStore key </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function createVSKey(findStr As String, replStr As String, key As iniKey) As iniKey

        Return New iniKey($"{key.Name}={key.Value.Replace(findStr, replStr)}")

    End Function

    ''' <summary> Processes a list of <c> winapp2entries </c> and removes any from the list that wouldn't be detected by CCleaner </summary>
    ''' <param name="entryList"> The list of <c> winapp2entries </c> who detection criteria will be audited </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Sub processEntryList(ByRef entryList As List(Of winapp2entry))

        ' If the entry's Detect criteria doesn't return true, prune it
        Dim sectionsToBePruned As New List(Of winapp2entry)
        entryList.ForEach(Sub(entry) If Not processEntryExistence(entry) Then sectionsToBePruned.Add(entry) Else virtualStoreChecker(entry))
        removeEntries(entryList, sectionsToBePruned)

    End Sub

    ''' <summary> Returns <c> True </c> if a SpecialDetect location exists, <c> False </c> otherwise </summary>
    ''' <param name="key"> A SpecialDetect format <c> iniKey </c> </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function checkSpecialDetects(ByVal key As String) As Boolean

        Select Case key

            Case "DET_CHROME"

                Dim detChrome As New List(Of String) _
                        From {"%AppData%\ChromePlus\chrome.exe", "%LocalAppData%\Chromium\Application\chrome.exe", "%LocalAppData%\Chromium\chrome.exe",
                        "%LocalAppData%\Flock\Application\flock.exe", "%LocalAppData%\Google\Chrome SxS\Application\chrome.exe", "%LocalAppData%\Google\Chrome\Application\chrome.exe",
                        "%LocalAppData%\RockMelt\Application\rockmelt.exe", "%LocalAppData%\SRWare Iron\iron.exe", "%ProgramFiles%\Chromium\Application\chrome.exe",
                        "%ProgramFiles%\SRWare Iron\iron.exe", "%ProgramFiles%\Chromium\chrome.exe", "%ProgramFiles%\Flock\Application\flock.exe",
                        "%ProgramFiles%\Google\Chrome SxS\Application\chrome.exe", "%ProgramFiles%\Google\Chrome\Application\chrome.exe", "%ProgramFiles%\RockMelt\Application\rockmelt.exe",
                        "HKCU\Software\Chromium", "HKCU\Software\SuperBird", "HKCU\Software\Torch", "HKCU\Software\Vivaldi"}

                For Each path In detChrome

                    If checkExist(path) Then Return True

                Next

            Case "DET_MOZILLA"

                Return checkPathExist("%AppData%\Mozilla\Firefox")

            Case "DET_THUNDERBIRD"

                Return checkPathExist("%AppData%\Thunderbird")

            Case "DET_OPERA"

                Return checkPathExist("%AppData%\Opera Software")

        End Select

        ' If we didn't return above, SpecialDetect definitely doesn't exist
        Return False

    End Function

    ''' <summary> Handles passing off checks from sources that may vary between file system and registry </summary>
    ''' <param name="path"> A filesystem or registry path whose existence will be audited </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function checkExist(path As String) As Boolean

        Return If(path.StartsWith("HK", StringComparison.InvariantCulture), checkRegExist(path), checkPathExist(path))

    End Function

    ''' <summary> Returns <c> True </c> if a given key exists in the Windows Registry, <c> False </c> otherwise </summary>
    ''' <param name="path"> A registry path to be audited for existence </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function checkRegExist(path As String) As Boolean

        Dim dir = path
        Dim root = getFirstDir(path)
        dir = dir.Replace(root & "\", "")
        Dim exists = getRegExists(root, dir)
        gLog($"{root}\{dir} exists", exists, indent:=True, buffr:=True)
        ' If we didn't return anything above, registry location probably doesn't exist
        Return exists

    End Function

    ''' <summary> Returns <c> True </c> if a given key exists in the registry, <c> False </c> otherwise </summary>
    ''' <param name="root"> The registry hive that contains the key whose existence will be audited </param>
    ''' <param name="dir"> The path of the key whose existence will be audited </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function getRegExists(root As String, dir As String) As Boolean

        Try

            Select Case root

                Case "HKCU"

                    Return getCUKey(dir) IsNot Nothing

                Case "HKLM"

                    If getLMKey(dir) IsNot Nothing Then Return True

                    ' Support checking for 32bit applications on Win64
                    dir = root + "\" + dir
                    dir = dir.ToUpperInvariant.Replace("HKLM\SOFTWARE", "SOFTWARE\WOW6432Node")
                    Return getLMKey(dir) IsNot Nothing

                Case "HKU"

                    Return getUserKey(dir) IsNot Nothing

                Case "HKCR"

                    Return getCRKey(dir) IsNot Nothing

                Case Else

                    ' Reject malformated keys
                    gLog($"Your key seems to be malformatted (bad root? - root: {root} - expected 'HKCU','HKLM','HKU' or 'HKCR')", indent:=True)
                    Return False

            End Select

        Catch ex As UnauthorizedAccessException

            ' The most common (only?) exception here is a permissions one, so assume true if we hit because a permissions exception implies the key exists anyway.
            Return True

        End Try

        Return True

    End Function




    ''' <summary> Handles some CCleaner variables and logs if the current variable is ProgramFiles so the 32bit location can be checked later </summary>
    ''' <param name="dir"> A filesystem path to process for environment variables </param>
    ''' <param name="isProgramFiles"> Indicates that the %ProgramFiles% variable has been seen </param>
    ''' Docs last updated: 2024-03-26- | Code last updated: 2024-03-26
    ''' <returns> <c> True </c>c> if an error occurred <br /> <c> False </c> otherwise </returns>
    Private Function processEnvDirs(ByRef dir As String, ByRef isProgramFiles As Boolean) As Boolean

        Dim errDetected = False

        If dir.Contains("%") Then

            Dim splitDir = dir.Split(CChar("%"))
            Dim var = splitDir(1)
            Dim envDir = Environment.GetEnvironmentVariable(var)
            Dim userProfileDir = Environment.GetEnvironmentVariable("UserProfile")
            Select Case var

                ' %ProgramFiles% in CCleaner points to both C:\Program Files and C:\Program Files (x86)
                ' This particular case is handled later in the trim process, we simply note it here for that purpose 
                Case "ProgramFiles"

                    isProgramFiles = True

                ' %Documents% is a CCleaner-only variable and points to two paths depending on system 
                ' Windows XP:       %UserProfile%\My Documents
                ' Windows Vista+:   %UserProfile%\Documents	
                Case "Documents"

                    envDir = $"{userProfileDir}\{If(winVer = 5.1 OrElse winVer = 5.2, "My ", "")}Documents"

                ' %CommonAppData% is a CCleaner-only variable which creates parity between the all users profile in windows xp and the programdata folder in vista+ 
                ' Windows XP:       %AllUsersProfile%\Application Data
                ' Windows Vista+    %AllUsersProfile%\
                Case "CommonAppData"

                    envDir = $"{Environment.GetEnvironmentVariable("AllUsersProfile")}\{If(winVer = 5.1 OrElse winVer = 5.2, "Application Data\", "")}"

                ' %LocalLowAppData% is a CCleaner-only variable which points to %UserProfile%\AppData\LocalLow
                Case "LocalLowAppData"

                    envDir = $"{Environment.GetEnvironmentVariable("LocalAppData").Replace("Local", "LocalLow")}"

                ' %Pictures% is a CCleaner-only variable which points to two paths depending on system 
                ' Windows XP:       %UserProfile%\My Documents\My Pictures
                ' Windows Vista+:   %UserProfile%\Pictures
                Case "%Pictures%"

                    envDir = $"{userProfileDir}\{If(winVer = 5.1 OrElse winVer = 5.2, "My Documents\My ", "")}Pictures"

                ' %Music% is a CCleaner-only variable which points to two paths depending on system 
                ' Windows XP:       %UserProfile%\My Documents\My Music
                ' Windows Vista+:   %UserProfile%\Music
                Case "%Music%"

                    envDir = $"{userProfileDir}\{If(winVer = 5.1 OrElse winVer = 5.2, "My Documents\My ", "")}Music"

                ' %Video% is a CCleaner-only variable which points to two paths depending on system 
                ' Windows XP:       %UserProfile%\My Documents\My Videos
                ' Windows Vista+:   %UserProfile%\Videos
                Case "%Video%"

                    envDir = $"{userProfileDir}\{If(winVer = 5.1 OrElse winVer = 5.2, "My Documents\My ", "")}Videos"

            End Select

            Try

                dir = envDir + splitDir(2)

            Catch ex As IndexOutOfRangeException

                errDetected = True

            End Try

        End If

        Return errDetected

    End Function

    ''' <summary> Returns <c> True </c> if a path exists on the file system, <c> False </c> otherwise </summary>
    ''' <param name="key"> A filesystem path </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function checkPathExist(key As String) As Boolean

        ' Make sure we get the proper path for environment variables
        Dim isProgramFiles = False
        Dim dir = key

        If processEnvDirs(dir, isProgramFiles) Then

            cwl("Error: " & key & " contains a malformatted environment variable and has been ignored")
            cwl("The associated entry will be retained in the final output file")
            cwl("Press any key to continue")
            crk()
            Return True

        End If

        Try

            ' Process wildcards appropriately if we have them
            If dir.Contains("*") Then

                Dim exists = expandWildcard(dir, True)

                ' Small contingency for the isProgramFiles case

                If Not exists AndAlso isProgramFiles Then

                    swapDir(dir, key)
                    exists = expandWildcard(dir, True)

                End If

                Return exists

            End If

            ' Check out those file/folder paths
            If Directory.Exists(dir) OrElse File.Exists(dir) Then Return True

            ' If we didn't find it and we're looking in Program Files, check the (x86) directory
            If isProgramFiles Then

                swapDir(dir, key)
                Dim exists = Directory.Exists(dir) OrElse File.Exists(dir)
                Return exists

            End If

        Catch ex As UnauthorizedAccessException

            Return True

        End Try

        Return False

    End Function

    ''' <summary> Swaps out a directory with the ProgramFiles parameterization on 64bit computers </summary>
    ''' <param name="dir"> The file system path to be modified </param>
    ''' <param name="key"> The original state of the path </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Sub swapDir(ByRef dir As String, key As String)

        Dim envDir = Environment.GetEnvironmentVariable("ProgramFiles(x86)")
        dir = envDir & key.Split(CChar("%"))(2)

    End Sub

    ''' <summary> Interprets parameterized wildcards for the current system </summary>
    ''' <param name="dir"> A path containing a wildcard </param>
    Private Function expandWildcard(dir As String, isFileSystem As Boolean) As Boolean

        gLog("Expanding Wildcard: " & dir, ascend:=True)

        ' This will handle wildcards anywhere in a path even though CCleaner only supports them at the end for DetectFiles
        Dim possibleDirs As New strList
        Dim currentPaths As New strList

        ' Split the given string into sections by directory
        Dim splitDir = dir.Split(CChar("\"))
        For Each pathPart In splitDir

            ' If this directory parameterization includes a wildcard, expand it appropriately
            ' This probably wont work if a string for some reason starts with a *
            If pathPart.Contains("*") Then

                For Each currentPath In currentPaths.Items

                    If currentPath.Length = 0 Then gLog(NameOf(currentPath) & " is empty, aborting wildcard expansion", descend:=True) : Return False

                    ' Query the existence of child paths for each current path we hold
                    If isFileSystem Then

                        gLog("Investigating: " & pathPart & " as a subdir of " & currentPath, indent:=True)

                        Try

                            ' If there are any possibilities, add them to our possibility list
                            Dim possibilities = Directory.GetDirectories(currentPath, pathPart)
                            possibleDirs.add(possibilities, possibilities.Any)

                        Catch ex As ArgumentException

                            ' These are thrown by currentPaths containing illegal characters, we'll assume this means the target doesn't exist
                            Return False

                        Catch ex As UnauthorizedAccessException

                            ' Assume that if there's some directory we don't have access to, the target exists and we just can't see it 
                            Return True

                        End Try

                    Else

                        ' Registry Query here

                    End If

                Next

                ' If no possibilities remain, the wildcard parameterization hasn't left us with any real paths on the system, so we may return false.

                If possibleDirs.Count = 0 Then gLog("Wildcard parameterization did not return any valid paths", descend:=True, buffr:=True) : Return False

                ' Otherwise, clear the current paths and repopulate them with the possible paths
                currentPaths.clear()
                currentPaths.add(possibleDirs)
                possibleDirs.clear()

            Else

                If currentPaths.Count = 0 Then

                    currentPaths.add($"{pathPart}")

                Else

                    Dim newCurPaths As New strList

                    For Each path In currentPaths.Items

                        If Not path.EndsWith("\", StringComparison.InvariantCulture) AndAlso Not path.Length = 0 Then path += "\"
                        newCurPaths.add($"{path}{pathPart}\", Directory.Exists($"{path}{pathPart}\"))

                    Next

                    currentPaths = newCurPaths
                    If currentPaths.Items.Count = 0 Then gLog("Wildcard parameterization did not return any valid paths", descend:=True) : Return False

                End If

            End If

        Next

        ' If any file/path exists, return true
        For Each currDir In currentPaths.Items

            If Directory.Exists(currDir) OrElse File.Exists(currDir) Then gLog($"Wildcard parameterization returned a valid path: {currDir}", descend:=True, buffr:=True) : Return True

        Next

        gLog(descend:=True)

        ' If we make it this far, the path does not exist 
        Return False

    End Function

    ''' <summary> Returns <c> True </c> if the system satisfies the DetectOS citeria, <c> False </c> otherwise </summary>
    ''' <param name="value"> The DetectOS criteria to be checked </param>
    ''' Docs last updated: 2022-11-21 | Code last updated: 2022-11-21
    Private Function checkDetOS(value As String) As Boolean

        Dim splitKey = value.Split(CChar("|"))

        ' There's three cases here: 
        ' |VERSION                  -> winVer > VERSION 
        ' VERSION|                  -> winver < VERSION 
        ' VERSION1|VERSION2         -> VERSION1 <= winVer <= VERSION2 
        Select Case True

            Case value.StartsWith("|", StringComparison.InvariantCultureIgnoreCase)

                Return Not winVer > Val(splitKey(1))

            Case value.EndsWith("|", StringComparison.InvariantCultureIgnoreCase)

                Return Not winVer < Val(splitKey(0))

            Case Else

                Return winVer >= Val(splitKey(0)) AndAlso winVer <= Val(splitKey(1))

        End Select

    End Function

End Module