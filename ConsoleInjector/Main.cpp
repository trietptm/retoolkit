// Retoolkit - Scripting-based reverse engineering toolkit for Windows OS'es
// Copyright (C) 2010  James Leskovar
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#include <boost/foreach.hpp>
#include <boost/program_options.hpp>
#include "Injector.h"

#define foreach BOOST_FOREACH

using namespace std;
using namespace boost::program_options;

int wmain(int argc, wchar_t* argv[])
{

  options_description desc("Injection options");
  desc.add_options()
    ("help,h", "produce help message")
    ("dll,d",  wvalue<wstring>(),  "specify dll to inject")
    ("forever,f", "keep running forever")
    ("program", wvalue<wstring>(), "specify program to force inject")
    ("progdelay", value<int>(), "ms to Sleep after proc creation")
    ("process,p",wvalue<vector<wstring> >(), "inject by process name")
    ("window,w", wvalue<vector<wstring> >(), "inject by window name")
    ("class,c",  wvalue<vector<wstring> >(), "inject by class name")
    ("pid", value<int>(), "inject by process id")
    ("delay,L",  value<int>()->default_value(MIN_DELAY_TIME), "set delay time")
    ("app-base", wvalue<wstring>()->default_value(L".", "."), "CLR app base to use")
    ("private-bin", wvalue<wstring>()->default_value(L"", "<default-ref>"), "CLR private path to use")
    ("param", wvalue<vector<wstring> >(), "Additional user parameter");

  positional_options_description podesc;
  podesc.add("param", -1);

  bool displayHelp = false;

  try
  {
    variables_map vm;
    wparsed_options parsed_opt = 
      wcommand_line_parser(argc, argv).options(desc).positional(podesc).allow_unregistered().run();

    store(parsed_opt, vm);
    notify(vm);

    if (vm.count("dll") == 0)
    {
      cout << "Error: Specify dll with --dll/-d" << endl;
      displayHelp = true;
    }
    else if (vm.count("help"))
      displayHelp = true;
    else
    {
      Injector injector;
      injector.setDelay(vm["delay"].as<int>());
      if (vm.count("forever")) 
        injector.setWaitFlag();

      if (vm.count("pid")) 
        injector.addPidMonitor(vm["pid"].as<int>());

      if (vm.count("process"))
        foreach (const wstring& s, vm["process"].as<vector<wstring> >())
        { injector.addProcMonitor(s); }

      if (vm.count("class"))
        foreach (const wstring& s, vm["class"].as<vector<wstring> >())
        { injector.addClassMonitor(s); }

      if (vm.count("window"))
        foreach (const wstring& s, vm["window"].as<vector<wstring> >())
        { injector.addWindowMonitor(s); }

      wstring userParam = L"";
      if (vm.count("param"))
      {
        foreach(const wstring& s, vm["param"].as<vector<wstring> >())
        { 
          userParam.append(s);
          userParam.append(L";");
        }
        userParam.erase(userParam.length()-1);
      }

      wstring program = L"";
      int delay = 0;
      if (vm.count("program"))
      {
        program = vm["program"].as<wstring>();
        if (vm.count("progdelay") > 0)
          delay = vm["progdelay"].as<int>();
      }
      
      wstring clrAppBase = vm["app-base"].as<wstring>();
      wstring clrPrivateBin = vm["private-bin"].as<wstring>();
      wstring dllName = vm["dll"].as<wstring>();

      injector.setProgramPath(program);
      injector.setProgramDelay(delay);
      injector.setDllName(dllName);
      injector.setAppBase(clrAppBase);
      injector.setPrivateBin(clrPrivateBin);
      injector.setUserParam(userParam);
      injector.monitor();
    }
  }
  catch (const exception& e)
  {
    cout << "ERROR: "<< e.what() << endl;
    displayHelp = true;
  }

  if (displayHelp)
    cout << desc << endl;

  return 0;
}
