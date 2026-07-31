# Convenience targets for running appbench programs under rpyforth.
# Runs via benchmark/run_appbench.sh.

RPYFORTH ?=
PROGRAMS := cd16sim brainless fcp lexex benchgc coremark

.PHONY: help $(PROGRAMS)

help:
	@echo "Usage: make -C appbench <program>   [program: $(PROGRAMS)]"
	@echo "       make -C appbench RPYFORTH=/path/to/rpyforth <program>"

$(PROGRAMS):
	RPYFORTH=$(RPYFORTH) ../benchmark/run_appbench.sh $@
