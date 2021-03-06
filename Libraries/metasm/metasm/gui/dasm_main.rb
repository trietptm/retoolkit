#    This file is part of Metasm, the Ruby assembly manipulation suite
#    Copyright (C) 2006-2009 Yoann GUILLOT
#
#    Licence is LGPL, see LICENCE in the top-level directory


module Metasm
module Gui
# the main disassembler widget: this is a container for all the lower-level widgets that actually render the dasm state
class DisasmWidget < ContainerChoiceWidget
	attr_accessor :dasm, :entrypoints, :gui_update_counter_max
	attr_accessor :keyboard_callback, :keyboard_callback_ctrl	# hash key => lambda { |key| true if handled }
	attr_accessor :clones, :idle_do_dasm
	attr_accessor :pos_history, :pos_history_redo
	attr_accessor :bg_color_callback	# proc { |address|  "rgb" # "00f" -> blue }
	attr_accessor :focus_changed_callback
	attr_accessor :parent_widget

	def initialize_widget(dasm, ep=[])
		@dasm = dasm
		@dasm.gui = self
		@entrypoints = ep
		@pos_history = []
		@pos_history_redo = []
		@keyboard_callback = {}
		@keyboard_callback_ctrl = {}
		@clones = [self]
		@parent_widget = nil

		addview :listing,   AsmListingWidget.new(@dasm, self)
		addview :graph,     GraphViewWidget.new(@dasm, self)
		addview :decompile, CdecompListingWidget.new(@dasm, self)
		addview :opcodes,   AsmOpcodeWidget.new(@dasm, self)
		addview :hex,       HexWidget.new(@dasm, self)
		addview :coverage,  CoverageWidget.new(@dasm, self)

		view(:listing).grab_focus
	end

	def start_disassembling
		@gui_update_counter_max = 100
		gui_update_counter = 0
		dasm_working = false
		@idle_do_dasm = true
		Gui.idle_add {
			# metasm disassembler loop
			# update gui once in a while
			dasm_working = true if not @entrypoints.empty? or not @dasm.addrs_todo.empty?
			if dasm_working
				protect {
					if not @dasm.disassemble_mainiter(@entrypoints)
						dasm_working = false
						gui_update_counter = @gui_update_counter_max
					end
				}
				gui_update_counter += 1
				if gui_update_counter > @gui_update_counter_max
					gui_update_counter = 0
					gui_update
				end
			end
			@idle_do_dasm
		}

		@dasm.callback_prebacktrace ||= lambda { Gui.main_iter }
	end

	def terminate
		@clones.delete self
		@idle_handle = nil if @clones.empty?
	end

	# returns the address of the item under the cursor in current view
	def curaddr
		curview.current_address
	end

	# returns the object under the cursor in current view (@dasm.decoded[curaddr])
	def curobj
		@dasm.decoded[curaddr]
	end

	# returns the address of the label under the cursor or the address of the line of the cursor
	def pointed_addr
		hl = curview.hl_word
		if hl =~ /^[0-9].*h$/ and a = hl.to_i(16) and @dasm.get_section_at(a)
			return a
		end
		@dasm.prog_binding[hl] || curview.current_address
	end


	def normalize(addr)
		case addr
		when ::String
			if @dasm.prog_binding[addr]
				addr = @dasm.prog_binding[addr]
			elsif (?0..?9).include? addr[0] or (?a..?f).include? addr.downcase[0]
				case addr
				when /^0x/i
				when /h$/i; addr = '0x' + addr[0...-1]
				when /[a-f]/i; addr = '0x' + addr
				when /^[0-9]+$/
					addr = '0x' + addr if not @dasm.get_section_at(addr.to_i) and
								  @dasm.get_section_at(addr.to_i(16))
				end
				begin
					addr = Integer(addr)
				rescue ::ArgumentError
					return
				end
			else
				return
			end
		end
		addr
	end

	def focus_addr(addr, viewidx=nil, quiet=false)
		viewidx ||= curview_index || :listing
		return if not addr
		return if viewidx == curview_index and addr == curaddr
		oldpos = [curview_index, (curview.get_cursor_pos if curview)]
		if [viewidx, oldpos[0], *view_indexes].compact.uniq.find { |i|
			o_p = view(i).get_cursor_pos
			if view(i).focus_addr(addr)
				view(i).gui_update if i != oldpos[0]
				showview(i)
				true
			else
				view(i).set_cursor_pos o_p
				false
			end
		}
			@pos_history << oldpos if oldpos[0]	# ignore start focus_addr
			@pos_history_redo.clear
			true
		else
			messagebox "Invalid address #{addr}" if not quiet
			if oldpos[0]
				showview oldpos[0]
				curview.set_cursor_pos oldpos[1]
			end
			false
		end
	end

	def focus_addr_back(val = @pos_history.pop)
		return if not val
		@pos_history_redo << [curview_index, curview.get_cursor_pos]
		showview val[0]
		curview.set_cursor_pos val[1]
		true
	end

	def focus_addr_redo
		# undo focus_addr_back
		if val = @pos_history_redo.pop
			@pos_history << [@notebook.page, curview.get_cursor_pos]
			@notebook.page = val[0]
			curview.set_cursor_pos val[1]
		end
	end

	def gui_update
		@clones.each { |c| c.do_gui_update }
	end

	def do_gui_update
		curview.gui_update	# invalidate all views ?
	end

	def redraw
		curview.redraw
	end

	def keep_focus_while
		addr = curaddr
		yield
		focus_addr curaddr if addr
	end
	
	# add/change a comment @addr
	def add_comment(addr)
		cmt = @dasm.comment[addr].to_a.join(' ')
		if @dasm.decoded[addr].kind_of? DecodedInstruction
			cmt += @dasm.decoded[addr].comment.to_a.join(' ')
		end
		inputbox("new comment for #{Expression[addr]}", :text => cmt) { |c|
			c = c.split("\n")
			c = nil if c == []
			if @dasm.decoded[addr].kind_of? DecodedInstruction
				@dasm.decoded[addr].comment = c
			else
				@dasm.comment[addr] = c
			end
			gui_update
		}
	end

	# disassemble from this point
	# if points to a call, make it return
	def disassemble(addr)
		if di = @dasm.decoded[addr] and di.kind_of? DecodedInstruction and di.opcode.props[:saveip]
			di.block.each_to_normal { |t|
				t = @dasm.normalize t
				next if not @dasm.decoded[t]
				@dasm.function[t] ||= @dasm.function[:default] ? @dasm.function[:default].dup : DecodedFunction.new
			}
			di.block.add_to_subfuncret(di.next_addr)
			@dasm.addrs_todo << [di.next_addr, addr, true]
		elsif addr
			@dasm.addrs_todo << [addr]
		end
	end

	# disassemble fast from this point (don't dasm subfunctions, don't backtrace)
	def disassemble_fast(addr)
		@dasm.disassemble_fast(addr)
		gui_update
	end

	# disassemble fast & deep from this point (don't backtrace, but still dasm subfuncs)
	def disassemble_fast_deep(addr)
		@dasm.disassemble_fast_deep(addr)
		gui_update
	end

	# (re)decompile
	def decompile(addr)
		if @dasm.c_parser and var = @dasm.c_parser.toplevel.symbol[addr] and (var.type.kind_of? C::Function or @dasm.decoded[@dasm.normalize(addr)].kind_of? DecodedInstruction)
			@dasm.decompiler.redecompile(addr)
			widget(:decompile).curaddr = nil
		end
		focus_addr(addr, :decompile)
	end

	def toggle_data(addr)
		return if @dasm.decoded[addr] or not @dasm.get_section_at(addr)
		@dasm.add_xref(addr, Xref.new(nil, nil, 1)) if not @dasm.xrefs[addr]
		@dasm.each_xref(addr) { |x|
			x.len = {1 => 2, 2 => 4, 4 => 8}[x.len] || 1
			break
		}
		gui_update
	end

	def list_functions
		list = [['name', 'addr']]
		@dasm.function.keys.each { |f|
			addr = @dasm.normalize(f)
			next if not @dasm.decoded[addr]
			list << [@dasm.get_label_at(addr), Expression[addr]]
		}
		title = "list of functions"
		listwindow(title, list) { |i| focus_addr i[1] }
	end

	def list_labels
		list = [['name', 'addr']]
		@dasm.prog_binding.each { |k, v|
			list << [k, Expression[@dasm.normalize(v)]]
		}
		listwindow("list of labels", list) { |i| focus_addr i[1] }
	end

	def list_sections
		list = [['addr', 'length', 'name', 'info']]
		@dasm.section_info.each { |n,a,l,i|
			list << [Expression[a], Expression[l], n, i]
		}
		listwindow("list of sections", list) { |i| focus_addr i[0] if i[0] != '0' or @dasm.get_section_at(0) }
	end

	def list_xrefs(addr)
		list = [['address', 'type', 'instr']]
		@dasm.each_xref(addr) { |xr|
			next if not xr.origin
			list << [Expression[xr.origin], "#{xr.type}#{xr.len}"]
			if di = @dasm.decoded[xr.origin] and di.kind_of? DecodedInstruction
				list.last << di.instruction
			end
		}
		if list.length == 1
			messagebox "no xref to #{Expression[addr]}" if addr
		else
			listwindow("list of xrefs to #{Expression[addr]}", list) { |i| focus_addr(i[0], nil, true) }
		end
	end

	# jump to address
	def prompt_goto
		inputbox('address to go', :text => Expression[curaddr]) { |v|
			if not focus_addr(v, nil, true)
				labels = @dasm.prog_binding.map { |k, vv|
 					[k, Expression[@dasm.normalize(vv)]] if k.downcase.include? v.downcase
				}.compact
				case labels.length
				when 0; focus_addr(v)
				when 1; focus_addr(labels[0][0])
				else
					labels.unshift ['name', 'addr']
					listwindow("list of labels", labels) { |i| focus_addr i[1] }
				end
			end
		}
	end

	def prompt_parse_c_file
		# parses a C header
		openfile('open C header') { |f|
			@dasm.parse_c_file(f) rescue messagebox("#{$!}\n#{$!.backtrace}")
		}
	end

	# run arbitrary ruby
	def prompt_run_ruby
		inputbox('ruby code to eval()') { |c| messagebox eval(c).inspect[0, 512], 'eval' }
	end

	# run ruby plugin
	def prompt_run_ruby_plugin
		openfile('ruby plugin') { |f| @dasm.load_plugin(f) }
	end

	# prompts for a new name for addr
	def rename_label(addr)
		old = addr
		if @dasm.prog_binding[old] or old = @dasm.get_label_at(addr)
			inputbox("new name for #{old}", :text => old) { |v|
				if v == ''
					@dasm.del_label_at(addr)
				else
					@dasm.rename_label(old, v)
				end
				gui_update
			}
		else
			inputbox("label name for #{Expression[addr]}", :text => Expression[addr]) { |v|
				next if v == ''
				@dasm.set_label_at(addr, v)
				if di = @dasm.decoded[addr] and di.kind_of? DecodedInstruction
					@dasm.split_block(di.block, di.address)
				end
				gui_update
			}
		end
	end

	# pause/play disassembler
	# returns true if playing
	# this empties @dasm.addrs_todo, the dasm may still continue to work if this msg is
	#  handled during an instr decoding/backtrace (the backtrace may generate new addrs_todo)
	# addresses in addrs_todo pointing to existing decoded instructions are left to create a prettier graph
	def playpause_dasm
		@dasm_pause ||= []
		if @dasm_pause.empty? and @dasm.addrs_todo.empty?
			true
		elsif @dasm_pause.empty?
			@dasm_pause = @dasm.addrs_todo.dup
			@dasm.addrs_todo.clear
			@dasm.addrs_todo.concat @dasm_pause.find_all { |a, *b| @dasm.decoded[@dasm.normalize(a)] }
			@dasm_pause -= @dasm.addrs_todo
			puts "dasm paused (#{@dasm_pause.length})"
		else
			@dasm.addrs_todo.concat @dasm_pause
			@dasm_pause.clear
			puts "dasm restarted (#{@dasm.addrs_todo.length})"
			true
		end
	end

	def toggle_expr_char(o)
		@dasm.toggle_expr_char(o)
		gui_update
	end

	def toggle_expr_offset(o)
		@dasm.toggle_expr_offset(o)
		gui_update
	end

	def toggle_view(idx)
		default = idx == :listing ? :graph : :listing
		focus_addr(curaddr, ((curview_index == idx) ? default : idx))
	end

	# undefines the whole function body
	def undefine_function(addr, incl_subfuncs = false)
		list = []
		@dasm.each_function_block(addr, incl_subfuncs) { |b| list << b }
		list.each { |b| @dasm.undefine_from(b) }
		gui_update
	end

	def keypress_ctrl(key)
		return true if @keyboard_callback_ctrl[key] and @keyboard_callback_ctrl[key][key]
		case key
		when :enter; focus_addr_redo
		when ?r; prompt_run_ruby
		when ?C; disassemble_fast_deep(curaddr)
		else return @parent_widget ? @parent_widget.keypress_ctrl(key) : false
		end
		true
	end

	def keypress(key)
		return true if @keyboard_callback[key] and @keyboard_callback[key][key]
		case key
		when :enter; focus_addr curview.hl_word
		when :esc; focus_addr_back
		when ?/; inputbox('search word') { |w|
				next unless curview.respond_to? :hl_word
				next if w == ''
				curview.hl_word = w 
				curview.redraw
			}
		when ?c; disassemble(curview.current_address)
		when ?C; disassemble_fast(curview.current_address)
		when ?d; toggle_data(curview.current_address)
		when ?f; list_functions
		when ?g; prompt_goto
		when ?l; list_labels
		when ?n; rename_label(pointed_addr)
		when ?o; toggle_expr_offset(curobj)
		when ?p; playpause_dasm
		when ?r; toggle_expr_char(curobj)
		when ?v; $VERBOSE = ! $VERBOSE ; puts "#{'not ' if not $VERBOSE}verbose"	# toggle verbose flag
		when ?x; list_xrefs(pointed_addr)
		when ?;; add_comment(curview.current_address)

		when ?\ ; toggle_view(:graph)
		when :tab; toggle_view(:decompile)
		else
			p key if $DEBUG
			return @parent_widget ? @parent_widget.keypress(key) : false
		end
		true
	end

	# creates a new dasm window with the same disassembler object, focus it on addr#win
	def clone_window(*focus)
		return if not popup = DasmWindow.new
		popup.display(@dasm, @entrypoints, :dont_dasm => true)
		w = popup.dasm_widget
		w.clones = @clones.concat w.clones
		w.idle_do_dasm = @idle_do_dasm
		w.focus_addr(*focus)
		popup
	end
end

class DasmWindow < Window
	attr_accessor :dasm_widget, :menu
	def initialize_window(title = 'metasm disassembler')
		self.title = title
		@dasm_widget = nil
	end

	def destroy_window
		@dasm_widget.terminate if @dasm_widget
		super()
	end

	# sets up a DisasmWidget as main widget of the window, replaces the current if it exists
	# returns the widget
	def display(dasm, ep=[], opts={})
		@dasm_widget.terminate if @dasm_widget
		@dasm_widget = DisasmWidget.new(dasm, ep)
		self.widget = @dasm_widget
		@dasm_widget.start_disassembling unless opts[:dont_dasm]
		@dasm_widget
	end

	# returns the specified widget from the @dasm_widget (idx in :hex, :listing, :graph etc)
	def widget(idx=nil)
		idx && @dasm_widget ? @dasm_widget.view(idx) : @dasm_widget
	end

	def loadfile(path)
		exe = Metasm::AutoExe.orshellcode { Metasm::Ia32.new }.decode_file(path) { |type, str|
			# Disassembler save file will use this callback with unhandled sections / invalid binary file path
			case type
			when 'binarypath'
				ret = nil
				openfile("please locate #{str}", :blocking => true) { |f| ret = f }
				return if not ret
				ret
			end
		}
		(@dasm_widget ? DasmWindow.new : self).display(exe.init_disassembler)
		exe
	end

	def build_menu
		# TODO dynamic checkboxes (check $VERBOSE when opening the options menu to (un)set the mark)
		filemenu = new_menu

		addsubmenu(filemenu, 'OPEN', '^o') {
			openfile('chose target binary') { |exename| loadfile(exename) }
		}
		addsubmenu(filemenu, '_Debug') {
			l = nil
			i = inputbox('chose target') { |name|
				i = nil ; l.destroy if l and not l.destroyed?
				if pr = OS.current.find_process(name)
					target = pr.debugger
				elsif name =~ /^(udp|tcp|.*\d+.*):/i	# don't match c:\kikoo, but allow 127.0.0.1 / [1:2::3]
					target = GdbRemoteDebugger.new(name)
				elsif pr = OS.current.create_process(name)
					target = pr.debugger
				else
					messagebox('no such target')
					next
				end
				DbgWindow.new(target)
			}

			# build process list in bg (exe name resolution takes a few seconds)
			list = [['pid', 'name']]
			list_pr = OS.current.list_processes
			Gui.idle_add {
				if pr = list_pr.shift
					path = pr.modules.first.path if pr.modules and pr.modules.first
					#path ||= '<unk>'	# if we can't retrieve exe name, can't debug the process
					list << [pr.pid, path] if path
					true
				elsif i
					l = listwindow('running processes', list, :noshow => true) { |e| i.text.buffer.text = e[0] }
					l.move(l.position[0] + l.size[0], l.position[1])
					l.show
					false
				end
			}
		}

		addsubmenu(filemenu, 'SAVE', '^s') {
			openfile('chose save file') { |file|
				@dasm_widget.dasm.save_file(file)
			} if @dasm_widget
		}

		addsubmenu(filemenu, 'CLOSE') {
			if @dasm_widget
				@dasm_widget.terminate
				@vbox.remove @dasm_widget
				@dasm_widget = nil
			end
		}
		addsubmenu(filemenu)

		iomenu = new_menu
		addsubmenu(iomenu, 'Load _map') {
			openfile('chose map file') { |file|
				@dasm_widget.dasm.load_map(File.read(file)) if @dasm_widget
			} if @dasm_widget
		}
		addsubmenu(iomenu, 'S_ave map') {
			savefile('chose map file') { |file|
				File.open(file, 'w') { |fd|
					fd.puts @dasm_widget.dasm.save_map
				} if @dasm_widget
			} if @dasm_widget
		}
		addsubmenu(iomenu, 'Save _C') {
			savefile('chose C file') { |file|
				File.open(file, 'w') { |fd|
					fd.puts @dasm_widget.dasm.c_parser
				} if @dasm_widget
			} if @dasm_widget
		}
		addsubmenu(filemenu, 'Load _C') {
			openfile('chose C file') { |file|
				@dasm_widget.dasm.parse_c(File.read(file)) if @dasm_widget
			} if @dasm_widget
		}
		addsubmenu(filemenu, '_i/o', iomenu)
		addsubmenu(filemenu)
		addsubmenu(filemenu, 'QUIT') { destroy } # post_quit_message ?

		addsubmenu(@menu, filemenu, '_File')

		# a fake unreferenced accel group, so that the shortcut keys appear in the menu, but the widget keypress is responsible
		# of handling them (otherwise this would take precedence and :hex couldn't get 'c' etc)
		# but ^o still works (must work even without DasmWidget loaded)
		hack_accel_group	# XXX

		actions = new_menu
		dasm = new_menu
		addsubmenu(dasm, '_Disassemble from here', 'c') { @dasm_widget.disassemble(@dasm_widget.curview.current_address) }
		hack_accel_group	# XXX ok, so an old gtk segfaults with an accelerator containing both c and C..
		addsubmenu(dasm, 'Disassemble _fast from here', 'C') { @dasm_widget.disassemble_fast(@dasm_widget.curview.current_address) }
		addsubmenu(dasm, 'Disassemble fast & dee_p from here') { @dasm_widget.disassemble_fast_deep(@dasm_widget.curview.current_address) }
		addsubmenu(actions, dasm, '_Disassemble')
		addsubmenu(actions, 'Follow', '<enter>') { @dasm_widget.focus_addr @dasm_widget.curview.hl_word }	# XXX
		addsubmenu(actions, 'Jmp back', '<esc>') { @dasm_widget.focus_addr_back }
		addsubmenu(actions, 'Undo jmp back', '^<enter>') { @dasm_widget.focus_addr_redo }
		addsubmenu(actions, 'Goto', 'g') { @dasm_widget.prompt_goto }
		addsubmenu(actions, 'List functions', 'f') { @dasm_widget.list_functions }
		addsubmenu(actions, 'List labels', 'l') { @dasm_widget.list_labels }
		addsubmenu(actions, 'List xrefs', 'x') { @dasm_widget.list_xrefs(@dasm_widget.pointed_addr) }
		addsubmenu(actions, 'Rename label', 'n') { @dasm_widget.rename_label(@dasm_widget.pointed_addr) }
		addsubmenu(actions, 'Decompile', '<tab>') { @dasm_widget.decompile(@dasm_widget.curview.current_address) }
		addsubmenu(actions, 'Decompile finali_ze') { @dasm_widget.dasm.decompiler.finalize ; @dasm_widget.gui_update }
		addsubmenu(actions, 'Comment', ';') { @dasm_widget.decompile(@dasm_widget.curview.current_address) }
		addsubmenu(actions, '_Undefine') { @dasm_widget.dasm.undefine_from(@dasm_widget.curview.current_address) ; @dasm_widget.gui_update }
		addsubmenu(actions, 'Unde_fine function') { @dasm_widget.undefine_function(@dasm_widget.curview.current_address) }
		addsubmenu(actions, 'Undefine function & _subfuncs') { @dasm_widget.undefine_function(@dasm_widget.curview.current_address, true) }
		addsubmenu(actions, 'Data', 'd') { @dasm_widget.toggle_data(@dasm_widget.curview.current_address) }
		addsubmenu(actions, 'Pause dasm', 'p', :check) { |ck| ck.active = !@dasm_widget.playpause_dasm }
		addsubmenu(actions, 'Run ruby snippet', '^r') {
			if @dasm_widget
				@dasm_widget.prompt_run_ruby
			else
				inputbox('code to eval') { |c| messagebox eval(c).inspect[0, 512], 'eval' }
			end
		}
		addsubmenu(actions, 'Run _ruby plugin') { @dasm_widget.prompt_run_ruby_plugin }

		addsubmenu(@menu, actions, '_Actions')

		options = new_menu
		addsubmenu(options, '_Verbose', :check, $VERBOSE, 'v') { |ck| $VERBOSE = ck.active? ; puts "#{'not ' if not $VERBOSE}verbose" }
		addsubmenu(options, 'Debu_g', :check, $DEBUG) { |ck| $DEBUG = ck.active? }
		addsubmenu(options, 'Debug _backtrace', :check) { |ck| @dasm_widget.dasm.debug_backtrace = ck.active? if @dasm_widget }
		addsubmenu(options, 'Backtrace li_mit') {
			inputbox('max blocks to backtrace', :text => @dasm_widget.dasm.backtrace_maxblocks) { |target|
				@dasm_widget.dasm.backtrace_maxblocks = Integer(target) if not target.empty?
			}
		}
		addsubmenu(options, 'Backtrace _limit (data)') {
			inputbox('max blocks to backtrace data (-1 to never start)',
					:text => @dasm_widget.dasm.backtrace_maxblocks_data) { |target|
				@dasm_widget.dasm.backtrace_maxblocks_data = Integer(target) if not target.empty?
			}
		}
		addsubmenu(options)
		addsubmenu(options, 'Forbid decompile _types', :check) { |ck| @dasm_widget.dasm.decompiler.forbid_decompile_types = ck.active? }
		addsubmenu(options, 'Forbid decompile _if/while', :check) { |ck| @dasm_widget.dasm.decompiler.forbid_decompile_ifwhile = ck.active? }
		addsubmenu(options, 'Forbid decomp _optimize', :check) { |ck| @dasm_widget.dasm.decompiler.forbid_optimize_code = ck.active? }
		addsubmenu(options, 'Forbid decomp optim_data', :check) { |ck| @dasm_widget.dasm.decompiler.forbid_optimize_dataflow = ck.active? }
		addsubmenu(options, 'Forbid decomp optimlab_els', :check) { |ck| @dasm_widget.dasm.decompiler.forbid_optimize_labels = ck.active? }
		addsubmenu(options, 'Decompiler _recurse', :check, true) { |ck| @dasm_widget.dasm.decompiler.recurse = (ck.active? ? 1/0.0 : 1) }	# XXX race if changed while decompiling
		# TODO CPU type, size, endian...
		# factorize headers

		addsubmenu(@menu, options, '_Options')

		views = new_menu
		addsubmenu(views, 'Dis_assembly') { @dasm_widget.focus_addr(@dasm_widget.curaddr, :listing) }
		addsubmenu(views, '_Graph') { @dasm_widget.focus_addr(@dasm_widget.curaddr, :graph) }
		addsubmenu(views, 'De_compiled') { @dasm_widget.focus_addr(@dasm_widget.curaddr, :decompile) }
		addsubmenu(views, 'Raw _opcodes') { @dasm_widget.focus_addr(@dasm_widget.curaddr, :opcodes) }
		addsubmenu(views, '_Hex') { @dasm_widget.focus_addr(@dasm_widget.curaddr, :hex) }
		addsubmenu(views, 'Co_verage') { @dasm_widget.focus_addr(@dasm_widget.curaddr, :coverage) }
		addsubmenu(views, '_Sections') { @dasm_widget.list_sections }

		addsubmenu(@menu, views, '_Views')
	end
end
end
end
