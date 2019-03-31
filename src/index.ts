import * as fs from 'fs'
import * as Path from 'path'
import { initialize, unInitialize, ProxyObject } from 'node-dotnet-bridge'

declare class ProcessorType {
    GetTest(text: string, number: Number, datetime: Date): string
    Add(a: number, b: number): number
}

let Processor: any

const log = function(text: string) { console.log(text) }
const deserialize = function (json: string) { 
    
    const objects: any[] = JSON.parse(json) 

    const getParameters = function(parameters: any[]) {
        return parameters.map(n => `${n.name}`).join(`, `)
    }

    const getMethods = function(methods: any[]) {
        return methods.map(n => `${n.name}(${getParameters(n.parameters)}) {
        return 8            
    }`).join(`
    `)
    }

    const objectScripts = objects.map(n => 
        `(class ${n.name} {
    ${getMethods(n.methods)}
})`).join(`

`)
    Processor = eval(objectScripts)    

    console.log(objectScripts)

    
    
    return JSON.parse(json) 
}

const resolveCoreclr = function(basePath: string, dllName: string) {
    const version = fs.readdirSync(basePath).sort((a, b) => - a.localeCompare(b))[0]
    const path = basePath + version
    const dllPath = Path.join(path, dllName)
    console.log("Loading dotnet core from " + dllPath)

	// Construct the trusted platform assemblies (TPA) list
	// This is the list of assemblies that .NET Core can load as
	// trusted system assemblies.
	// For this host (as with most), assemblies next to CoreCLR will
	// be included in the TPA list

    // This will add all files with a .dll extension to the TPA list.
	// This will include unmanaged assemblies (coreclr.dll, for example) that don't
	// belong on the TPA list. In a real host, only managed assemblies that the host
	// expects to load should be included. Having extra unmanaged assemblies doesn't
	// cause anything to fail, though, so this function just enumerates all dll's in
    // order to keep this sample concise.
    const sourcePath = Path.join(__dirname, "..")
    const sourceAssemblies = fs.readdirSync(sourcePath).filter(n => n.endsWith(".dll")).map(n => Path.join(sourcePath, n))
    const coreAssemblies = fs.readdirSync(path).filter(n => n.endsWith(".dll")).map(n => Path.join(path, n))
    const appPath = Path.join(__dirname, "..", "node_modules", "node-dotnet-bridge")
    const assemblies = [
        Path.join(appPath, "NodeDotnet.dll"),
        Path.join(appPath, "NodeDotnet.Core.dll")]
        .concat(sourceAssemblies)
        .concat(coreAssemblies)

    //assemblies.forEach(n => console.log(n))
    const tpaList = assemblies.join(Path.delimiter)
    //console.log(tpaList)

    console.log(__dirname)

    eval("global.testDate = new Date()")
    console.log(testDate.getFullYear())

    return {
        path: path,
        appPath: appPath,
        dll: dllPath,
        tpaList: tpaList
    }
}



// TODO: logCallback: kann auch null sein, dann kein Logging
initialize({
    logCallback: log,
    deserialize: deserialize,
    resolveCoreclr: resolveCoreclr
})

function multiObjects() {
    const proxy1 = new ProxyObject()    
    const proxy2 = new ProxyObject()
    const ret1 = proxy1.executeSync("👏👏")
    const ret2 = proxy2.executeSync("Das kömmt äüß Typescript😁")
}

const proxy = new ProxyObject()
multiObjects()
const ret = proxy.executeSync("Das kömmt äüß Typescript😁😁😁👏👏")

const processor: ProcessorType = new Processor() 
console.log(processor.GetTest("text", 23, new Date()))
console.log(processor.Add(1, 2))

for (let i = 0; i < 1000000; i++)
    proxy.executeSync(JSON.stringify({
        name: "Das kömmt äüß Typescript😁😁😁👏👏"
    }))

unInitialize()
console.log("Finished")


