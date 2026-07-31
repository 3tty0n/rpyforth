VERSION=1.4
PACKAGE=appbench-$(VERSION)

dist:
	rm -rf $(PACKAGE)
	cvs -d :ext:mips:/nfs/a5/anton/CVSroots/forth co bench
	mv bench $(PACKAGE)
	rm -rf $(PACKAGE)/bench-gc
	cd $(PACKAGE)/benchgc && cvs -d :ext:mips.complang.tuwien.ac.at:/usr/local/lib/cvs-repository/src-master co gforth/compat && mv gforth/compat compat && rm -rf gforth
	cd $(PACKAGE) && ln -s benchgc benchgc4
	cd $(PACKAGE) && cvs -d :ext:mips.complang.tuwien.ac.at:/nfs/a5/anton/CVSroots/forth co ansforth/extensions
	zip -9r $(PACKAGE).zip $(PACKAGE)
