using System.Text;
using TcpChatServer;

var str = "Hello";
var str2 = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aenean elit urna, molestie sed justo ut, dictum porta velit. Fusce feugiat placerat feugiat. Nulla ultrices viverra diam at scelerisque. Integer mattis efficitur dolor, id tempus purus laoreet ut. Aliquam ac consequat dolor. Suspendisse suscipit hendrerit congue. Vestibulum ultricies ante orci, ac consequat felis auctor in. Nulla rutrum diam at metus finibus, eget consectetur purus suscipit.

Nullam rutrum purus sit amet elit egestas efficitur. Nullam ullamcorper lorem iaculis arcu cursus, vel placerat augue feugiat. Donec odio lorem, tempor ut dictum eleifend, ullamcorper sed est. Nulla at maximus ex. Sed vel justo et nulla viverra suscipit non mollis mauris. Nulla ut vehicula est, quis dapibus diam. Nullam aliquet id est in dignissim. Vestibulum et posuere nisl, eget cursus lacus. Pellentesque posuere rhoncus lacinia. Aenean at egestas arcu, eu euismod nisl. Aenean felis eros, ullamcorper at laoreet eget, suscipit at neque. Ut vel commodo metus, aliquam lobortis nisl. Nullam consequat nec nulla vitae bibendum. Aenean nec laoreet elit. Etiam in tincidunt mauris. Nullam lectus risus, tincidunt a est eu, dignissim maximus diam.

Nam ac nisl aliquet lacus blandit placerat. Mauris non gravida tellus. Suspendisse non pulvinar nisi. Sed hendrerit sit amet sapien ac rhoncus. Sed ultricies felis eu pretium faucibus. Pellentesque luctus turpis in quam iaculis, vitae consequat turpis consequat. Cras facilisis dolor et ipsum pharetra condimentum. Vivamus vel neque mi. Fusce fringilla semper feugiat. Praesent aliquam eleifend nibh quis hendrerit. Aenean iaculis, nunc et eleifend gravida, lacus tortor consequat diam, in ornare felis sem et dolor. Morbi maximus malesuada eros, sit amet rhoncus ex egestas non.

Vivamus malesuada nunc eu eros lacinia euismod. Pellentesque posuere erat vel enim aliquam maximus. Donec pellentesque finibus nibh, quis aliquet arcu pharetra ut. Vestibulum imperdiet, eros non euismod feugiat, turpis metus bibendum diam, eu feugiat urna nibh in nulla. Aliquam gravida, tellus vel tempus ullamcorper, mauris mauris facilisis ligula, non ullamcorper ipsum velit vitae ligula. Duis dignissim pharetra orci et rhoncus. Donec et urna at ante gravida feugiat quis a eros. Phasellus a libero in massa scelerisque tempor. Donec nunc erat, scelerisque quis vulputate vel, condimentum ac dui. Nulla sollicitudin diam scelerisque dui vulputate, sed ultrices dui commodo. Cras ante nunc, ultricies at consequat quis, pellentesque vel ante. Integer eu ullamcorper nibh. Mauris purus diam, hendrerit commodo sem non, fringilla laoreet ipsum. Aliquam vitae libero mollis, bibendum justo gravida, ullamcorper tortor. Pellentesque pulvinar fermentum magna, ac mattis felis tincidunt nec. Sed accumsan ultrices ex a dignissim.

Donec sed efficitur justo. Donec ligula ligula, sollicitudin ac tortor et, elementum sollicitudin lectus. Quisque consequat congue rhoncus. Phasellus quis egestas libero, sed pharetra elit. Vestibulum varius iaculis blandit. Vivamus vel placerat nunc. Sed ac tincidunt augue, eu fermentum erat. Donec eu augue mauris. Proin non augue a nisl ultricies sagittis.

Vestibulum viverra congue elementum. Aliquam vestibulum lorem nec suscipit mattis. Mauris magna lorem, molestie non viverra ac, efficitur eu sapien. Suspendisse porttitor bibendum dolor, ac sodales urna fermentum nec. Cras eget luctus velit, eu imperdiet erat. Nam bibendum, velit a euismod feugiat, leo tellus pellentesque sapien, eu iaculis velit risus et sapien. Morbi iaculis, justo ac venenatis consectetur, elit turpis mollis ante, non venenatis leo nulla et tellus. Sed euismod tincidunt elit, quis efficitur odio lobortis vitae. Integer id arcu fringilla, molestie augue vitae, consequat nibh. In scelerisque velit et cursus commodo. Curabitur fermentum orci sed condimentum vulputate. Aliquam eu ornare quam. Suspendisse condimentum mi egestas, volutpat augue id, suscipit ligula. Donec vel dapibus nunc. Integer ultrices vulputate mollis.

Nullam turpis mi, faucibus id diam at, elementum malesuada turpis. Vestibulum quis orci nunc. Morbi nulla magna, maximus at ligula ac, dapibus placerat nibh. Vestibulum at risus auctor, consectetur urna nec, aliquet dolor. Quisque eget volutpat augue. Sed ac eros nec dui faucibus euismod ac sagittis lectus. Nam volutpat nibh purus, quis malesuada felis porttitor at. Nam feugiat mollis tortor sit amet maximus. Mauris pretium velit justo, a convallis eros accumsan quis. Curabitur eleifend viverra iaculis. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Suspendisse elementum tempus pretium. Aliquam facilisis faucibus dolor sit amet accumsan. Suspendisse potenti. Suspendisse potenti.

Sed non mi tincidunt lectus lobortis tempus vel ut eros. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Mauris mattis sapien arcu, a iaculis nibh iaculis a. Duis commodo arcu ac posuere sodales. Aliquam molestie accumsan magna, quis iaculis risus tristique sed. Sed pretium sapien sit amet orci efficitur, sit amet consectetur dolor consectetur. Curabitur laoreet ante ut lacus facilisis efficitur. Fusce dapibus nunc nisl, ut egestas diam fringilla et. Quisque pellentesque faucibus arcu, at euismod ex ornare non. Mauris tincidunt, nisl vitae placerat ultricies, tellus turpis varius urna, eget interdum lorem augue nec odio. Nunc sit amet gravida lacus, eu accumsan ligula. Suspendisse egestas risus at nibh vehicula, vitae congue eros finibus. Quisque lacinia fermentum nisi.

Donec dapibus augue iaculis nunc consequat vestibulum. Aenean vitae lacus posuere, bibendum ligula eu, rutrum velit. Sed ut sem nec nibh hendrerit pulvinar. Ut eleifend accumsan consequat. Nam ut ligula nec diam pretium fermentum et ac tellus. Vivamus bibendum varius dui sed semper. Praesent egestas nisi maximus lorem bibendum, et hendrerit purus porttitor. Maecenas leo lacus, tempor at faucibus non, feugiat eget eros. Phasellus ante diam, tempor ac laoreet ac, varius et dui. Pellentesque vehicula aliquam diam vel iaculis.

Proin lobortis tristique lectus, vel vulputate dui rutrum vitae. Integer sit amet turpis sollicitudin, tristique leo efficitur, ultricies tortor. Donec pharetra auctor urna, a gravida erat tincidunt eget. Lorem ipsum dolor sit amet, consectetur adipiscing elit. In magna ligula, lobortis quis pretium et, rhoncus et justo. Vivamus aliquet metus sed placerat luctus. Proin eu felis tristique, volutpat nulla vitae, consectetur magna. Aenean elit leo, scelerisque scelerisque mattis a, dapibus aliquet urna. Integer ac libero gravida, vehicula metus sed, viverra lacus. Cras condimentum elit id feugiat fermentum.";

var bytes = Encoding.UTF8.GetBytes(str2);

var packages = new List<byte[]>();

if (bytes.Length > PackageBuilder.MaxSizeOfContent)
{
    var chunks = bytes
        .Chunk(PackageBuilder.MaxSizeOfContent).ToList();

    var chuncksCount = chunks.Count;

    for (var i = 0; i < chuncksCount; i++)
    {
        var builder = new PackageBuilder(chunks[i].Length)
            .WithCommand(SupportCommand.Hello);

        builder = builder.WithFullness(i == chuncksCount - 1
            ? FullnessPackage.Full
            : FullnessPackage.Partial);
        
        packages.Add(builder
            .WithContent(chunks[i])
                .Build());
    }
}
else
{
    packages.Add(new PackageBuilder(bytes.Length)
        .WithCommand(SupportCommand.Hello)
        .WithFullness(FullnessPackage.Full)
        .WithContent(bytes)
        .Build());
}


var resultMessage = new List<byte>();

packages.ForEach(p => resultMessage.AddRange(p.Skip(10).SkipLast(1)));

var message = Encoding.UTF8.GetString(resultMessage.ToArray());
Console.WriteLine(message);

