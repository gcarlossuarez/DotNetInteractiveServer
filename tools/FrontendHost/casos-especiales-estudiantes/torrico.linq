<Query Kind="Program" />

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class Localidad
    {
        public string Nombre {get;set;}
        public int Testigos{get;set;}
        public int Soldados {get;set;}
        
        public Localidad(string nombre,int testigos,int soldado)
            {
            Nombre=nombre;
            Testigos=testigos;
            Soldados=soldado;
            }
    }
   
class Program
{ 
    static void Main(string[] args)
    {
        Console.OutputEncoding=Encoding.UTF8;
        string carpeta = @"D:\Universidad Católica Boliviana\AlgoritmosEjemplo\Problema de la Mochila\Test\OttoSkorzeny\DataSet";

         

           if(Directory.Exists(carpeta))
            {     
             string[] archivos=Directory.GetFiles(carpeta,"dato*.txt");
             Array.Sort(archivos,StringComparer.Ordinal);
                 foreach(string ruta in archivos)
                {
                    ProcesarArchivo(ruta);
                }
            }
    }

    static void ProcesarArchivo(string ruta)
    {
        String[] lineas=File.ReadAllLines(ruta,Encoding.UTF8);
        int soldadosDisponibles=-1;
        List<Localidad>localidades=new List<Localidad>();
        for(int i=0;i<lineas.Length; i++)
        {
            string linea =lineas[i].Trim();
                if(string.IsNullOrWhiteSpace(linea))
                continue;

                if(soldadosDisponibles==-1)
                {    
                    if(int.TryParse(linea,out int capacidad))
                    {
                    soldadosDisponibles=capacidad;
                    continue;
                    }
                    else        
                    {
                       return;
                    }
                }
                string[]partes=linea.Split(';');
                if(partes.Length>=3)
                {
                    string nombre=partes[0].Trim();
                    bool okTestigo=int.TryParse(partes[1].Trim(), out int testigos);
                    bool okSoldados=int.TryParse(partes[2].Trim(),out int soldados);
                
                    if(okTestigo && okSoldados)
                    {
                        localidades.Add(new Localidad(nombre,testigos,soldados));
                    }
                }
        }
        if(soldadosDisponibles<0)
            return;
            ResolverMochila01(soldadosDisponibles,localidades);
        }

        static void ResolverMochila01(int capacidadMaxima,List<Localidad> items)
        {
            int n=items.Count;
            int[,] dp =new int[n+1,capacidadMaxima+1];
            for(int i=1;i<=n;i++)
            {
                int peso=items[i-1].Soldados;
                int valor=items[i-1].Testigos;
                
                for(int w=0;w<=capacidadMaxima;w++)
                {
                    if(peso<=w)
                       {
                            int incluir=valor+dp[i-1,w-peso];
                            int excluir=dp[i-1,w];    
                            dp[i,w]=Math.Max(incluir,excluir);
                        }
                        else
                        {    
                            dp[i,w]=dp[i-1,w];
                        }
                }
            }

            int maxTestigos=dp[n,capacidadMaxima];
            List<string>elegidos=new List<string>();
            int wActual=capacidadMaxima;
            
            for(int i=n;i>0 &&  wActual>=0;i--)
            {
                if(dp[i,wActual] !=dp[i-1,wActual])
                    {
                        Localidad loc=items[i-1];
                        //elegidos.Add($"{loc.Nombre} {loc.Testigos} {loc.Soldados}");
						elegidos.Add($"{loc.Nombre}-{loc.Testigos} {loc.Soldados}");
                        wActual-=loc.Soldados;
                    }
            }
			//String[] salidaOrdenada=elegidos.ToArray();
            String[] salidaOrdenada=elegidos.ToArray().OrderBy(x=> x.Split('-')[0]).ToArray();
            Array.Sort(salidaOrdenada, StringComparer.Ordinal);
            Console.WriteLine(maxTestigos);

            for(int i=0; i< salidaOrdenada.Length;i++)
            {
                //Console.WriteLine(salidaOrdenada[i]);
				Console.WriteLine(salidaOrdenada[i].Replace("-", " "));
            }
        }
}

