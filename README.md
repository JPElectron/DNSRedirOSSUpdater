# DNSRedirOSSUpdater

This readme is for DNS Redirector OSS Updater - this replaces the old DNS Redirector site "FAQ 52: Using updater.exe to automatically get new keywords"

Used to update the Internet filtering capability of DNS Redirector; it can retrieve keyword lists provided by me, 3rd party sites, your own custom files, and then combine the lists.

The latest version from GitHub is: v2.3.0.2 r10/02/2020 (requires .NET Framework 4.6.1)

You can download it here: https://github.com/JPElectron/DNSRedirOSSUpdater/raw/main/updater.exe

    You MUST unblock this package before extracting
    Right-click on the updater.exe you downloaded, select Properties, click the Unblock button
       ...if this button is not present just proceed, click OK
    Copy to the working directory C:\DNSREDIR
    Delete your C:\DNSREDIR\UpdaterCache folder (it will get re-created automatically on the next run)
    Run updatersetup.bat, verify your block list selections, press OK
    Verify your scheduled task is set to run updater.exe once a day
    Optionally, run updater.exe to do an update now

This same Updater application and associated files are included in the DNSRedirOSS repository .zip


## Related repositories

https://github.com/JPElectron/DNSRedirOSS

https://github.com/JPElectron/keywords


## Documentation

updater.exe assumes BlockedKeywordsFile=blocked.txt in dnsredir.ini (because blocked.txt is the output file) and that you're using DNS Redirector v7.2.x or later.

Don't add your own keywords to blocked.txt directly because they will be overwritten when the update is run. For this purpose, create a blocked-custom.txt file in the working directory and specify this filename under "Include these custom files:" then check the box beside it so it gets incorporated into blocked.txt

With updater.exe v2.3.0.2 options to download and create an allowed.txt and/or nxdforce.txt are also available.

    [DEPRECIATED INFO from the old DNS Redirector site]
      With updater.exe v2.2.x or later, when a DNS Redirector license is found, additional options to download and create an allowed.txt and/or nxdforce.txt file becomes available.
    [/DEPRECIATED INFO]

When updater.exe is run the download and merge procedure will start.
When updater.exe /setup is run (or use updatersetup.bat) the selection GUI is shown, click OK to save any changes.

Pick update from server "https://raw.githubusercontent.com/JPElectron/keywords/master/"

    [DEPRECIATED INFO from the old DNS Redirector site]
      Pick update from server "http://updt.dnsredirctrl.com" This is hosted on Amazon S3 for redundancy and global availability.
    [/DEPRECIATED INFO] This domain will fail to resolve at the end of 2020.

Suggested: Check "Run updaterdone.bat when done" to restart the service when done
With DNS Redirector v7.2.x or later a restart is not required, the new blocked.txt (or allowed.txt or nxdforce.txt) will be reloaded automatically. However in some cases restarting can free up server memory and has the benefit of "forgetting" all clients so they must become authorized and/or bypass the block the next day.

Other examples you might use the updaterdone.bat file for...
- stop the exe, copy in an alternate .ini set to block everything, then start, manually change to the updated list later
- send an email that the update was done, attaching the log, manually change to the updated list later
- copy the latest blocked.txt to other servers
- restart processes locally/remotely using pskill.exe, psexec.exe or sc.exe
- stop and start multiple services

Use a scheduled task to run updater.exe every X days... 
Usually late at night (after 11pm CST is suggested, when your network is least active, there is no advantage to running it more than once a day)
Administrative Tools > Task Scheduler
...define the program as C:\DNSREDIR\updater.exe and the start in or working directory as C:\DNSREDIR


## License

GPL does not allow you to link GPL-licensed components with other proprietary software (unless you publish as GPL too).

GPL does not allow you to modify the GPL code and make the changes proprietary, so you cannot use GPL code in your non-GPL projects.

If you wish to integrate this software into your commercial software package, or you are a corporate entity with more than 10 employees, then you should obtain a per-instance license, or a site-wide license, from http://jpelectron.com/buy


## Version History

v2.3.0.2 r10/02/2020
 - The only download URL is now "https://raw.githubusercontent.com/JPElectron/keywords/master/"
 - No .md5 file check for provided block list files (the days of dialup connections are behind us)
 - No license file check
 - Requires .NET Framework 4.6.1

v2.2.0.1 r07/27/2017
 - Added Allowed and NXDForce download and consolidate group options (tabs only show up when dnsredir.lic file found over 2bytes in wkdir)
 - Added command-line options to set CPU usage: /Lowest /BelowNormal /Normal /AboveNormal /Highest
 - Note if /keywords/updater/verchk.txt is not found/downloaded then software exits
 - Requires .NET Framework 4.6.1 ...performance and security enhancements (Server 2003 support is gone)

v2.1.0.8 r10/15/2012

 - Removed direct download from phishtank using api key, due to rate limiting
 - Added advert2.txt list selection, includes all verified badware domains regardless of ccTLD
 - Requires .NET Framework 4.0 Client Profile
 
v2.1.0.1 r03/15/2011

 - Fixed when including custom files blank lines/spaces do not get removed during consolidation
 - Fixed the comment character ; before a regex keyword does not remove the line during consolidation
 - Requires .NET Framework 4.0 Client Profile
 
v2.0.0.2 12/05/2009

 - Added Check download consistency via .md5 file
 - Added 5 custom file boxes instead of 3
 - Added "Include keywords aiding lack of NXDomain" blocklist
 - Added PhishTank data retrieval
 - Updater rebuilt, requires .NET Framework 2.0


[End of Line]
