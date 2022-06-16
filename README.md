# GetManuals

<p>Windows service to synchronize a destination folder with a source folder.</p>
<p>Coded to supply offline(no internet, but local intranet) computers with up to date company manuals, primarily .pdf/.doc/.. files</p>
<p>Performs this:</p>
<ul>
  <li>Checks for new files on source, copy to destination if missing.</li>
  <li>Checks if newer version of existing files are present on source, deletes old one on destination and copies newer to destination</li>
  <li>If newer file exist, but current file is in use at destination(i.e. locked file), the check will skip copying and try again in set check interval</ul>
</ul>

<p>Need to be edited for own use (in GetManualsService.cs):</p>
<pre><code class="language-cs">// Define remote URL to where the source is.
private string remoteUrl = @"\\xxx.xxx.xxx.xxx\Offshore_Manuals\Manuals";
// Local folder to use as destination and sync folder.
private string localUrl = @"D:\Manuals";</code></pre>

<p>Service must be configured with local user that has access to remote file server</p>
<p>Popup for this will show when installing the service using Install.bat</p>
<p>Can edit following in ProjectInstaller.Designer.cs:</p>
<pre><code class="language-cs">// Uncomment this to enable auto login when installing service, edit with correct username and password.<br>
//this.serviceProcessInstaller1.Password = @"RemoteSourceAccess(LocalUser)";<br>
//this.serviceProcessInstaller1.Username = @"PasswordToAccess";</code></pre>

<p>Check runs on a set interval of 6 hours, can be changed in GetManualsService.cs</p>

<pre><code class='language-cs'>// Timer for running OnTimer method
    Timer timer = new Timer
    {
        Interval = 60000 * 60 * 6 // 6 hours
    };
</code></pre>
